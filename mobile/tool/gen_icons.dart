// ignore_for_file: avoid_print
// One-off generator: crops the eagle mark out of assets/logo.jpg and produces
// transparent-background assets used for the in-app logo and launcher icon.
//
// Run with:  dart run tool/gen_icons.dart
//
// Outputs:
//   assets/logo_mark.png        eagle, transparent bg, square (in-app logo)
//   assets/icon.png             1024² white bg, eagle centered (legacy icon)
//   assets/icon_foreground.png  1024² transparent, eagle in adaptive safe zone
import 'dart:io';
import 'package:image/image.dart' as img;

void main() {
  final src = img.decodeImage(File('assets/logo.jpg').readAsBytesSync());
  if (src == null) {
    stderr.writeln('Could not read assets/logo.jpg');
    exit(1);
  }
  print('source: ${src.width}x${src.height}');

  // The wordmark sits in a near-solid black bar lower down. Find the top of
  // that bar (first wide dark row below 30% height) and only scan above it so
  // we isolate the eagle.
  int scanBottom = src.height;
  for (var y = (src.height * 0.30).round(); y < src.height; y++) {
    var dark = 0;
    for (var x = 0; x < src.width; x++) {
      final p = src.getPixel(x, y);
      if ((0.299 * p.r + 0.587 * p.g + 0.114 * p.b) < 90) dark++;
    }
    if (dark / src.width > 0.4) {
      // Trim a small margin so the bar's anti-aliased top edge isn't included.
      scanBottom = (y - src.height * 0.015).round();
      break;
    }
  }
  print('wordmark bar top: $scanBottom');

  // Auto-detect the eagle's bounding box from dark pixels.
  int minX = src.width, minY = src.height, maxX = 0, maxY = 0;
  bool found = false;
  for (var y = 0; y < scanBottom; y++) {
    for (var x = 0; x < src.width; x++) {
      final p = src.getPixel(x, y);
      final lum = (0.299 * p.r + 0.587 * p.g + 0.114 * p.b);
      if (lum < 110) {
        found = true;
        if (x < minX) minX = x;
        if (y < minY) minY = y;
        if (x > maxX) maxX = x;
        if (y > maxY) maxY = y;
      }
    }
  }
  if (!found) {
    stderr.writeln('No dark pixels found — is the logo as expected?');
    exit(1);
  }
  // Pad the box a little.
  final pad = (src.width * 0.03).round();
  minX = (minX - pad).clamp(0, src.width - 1);
  minY = (minY - pad).clamp(0, src.height - 1);
  maxX = (maxX + pad).clamp(0, src.width - 1);
  maxY = (maxY + pad).clamp(0, src.height - 1);
  final cropW = maxX - minX + 1;
  final cropH = maxY - minY + 1;
  print('eagle bbox: ($minX,$minY) ${cropW}x$cropH');

  final cropped = img.copyCrop(src, x: minX, y: minY, width: cropW, height: cropH);

  // Convert near-white to transparent so the mark sits on any background.
  final transparent = cropped.convert(numChannels: 4);
  for (var y = 0; y < transparent.height; y++) {
    for (var x = 0; x < transparent.width; x++) {
      final p = transparent.getPixel(x, y);
      final lum = (0.299 * p.r + 0.587 * p.g + 0.114 * p.b);
      if (lum > 225) {
        transparent.setPixelRgba(x, y, 0, 0, 0, 0); // fully transparent
      }
    }
  }

  // logo_mark.png — square transparent canvas, mark centered.
  _square(transparent, 1024, fill: 0x00000000, scale: 0.92).pngTo('assets/logo_mark.png');

  // icon_foreground.png — adaptive foreground, mark in ~62% safe zone.
  _square(transparent, 1024, fill: 0x00000000, scale: 0.62).pngTo('assets/icon_foreground.png');

  // icon.png — legacy launcher icon, white background.
  _square(transparent, 1024, fill: 0xFFFFFFFF, scale: 0.74).pngTo('assets/icon.png');

  print('done.');
}

/// Centers [mark] inside a [size]² canvas, scaled to [scale] of the canvas.
img.Image _square(img.Image mark, int size, {required int fill, required double scale}) {
  final canvas = img.Image(width: size, height: size, numChannels: 4);
  img.fill(canvas, color: img.ColorInt32.rgba(
    (fill >> 16) & 0xFF, (fill >> 8) & 0xFF, fill & 0xFF, (fill >> 24) & 0xFF));

  final target = (size * scale).round();
  final ratio = mark.width / mark.height;
  int w, h;
  if (ratio >= 1) {
    w = target;
    h = (target / ratio).round();
  } else {
    h = target;
    w = (target * ratio).round();
  }
  final resized = img.copyResize(mark, width: w, height: h, interpolation: img.Interpolation.cubic);
  final dx = ((size - w) / 2).round();
  final dy = ((size - h) / 2).round();
  img.compositeImage(canvas, resized, dstX: dx, dstY: dy);
  return canvas;
}

extension on img.Image {
  void pngTo(String path) {
    File(path).writeAsBytesSync(img.encodePng(this));
    print('wrote $path (${width}x$height)');
  }
}
