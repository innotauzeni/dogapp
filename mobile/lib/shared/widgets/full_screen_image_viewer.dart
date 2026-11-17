import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:photo_view/photo_view.dart';

import '../../core/utils/photo_util.dart';

/// Full-screen image viewer with pinch-to-zoom and pan
class FullScreenImageViewer extends StatelessWidget {
  final String? imageUrl;
  final String? heroTag;

  const FullScreenImageViewer({
    super.key,
    required this.imageUrl,
    this.heroTag,
  });

  static Future<void> show(BuildContext context, String? imageUrl, {String? heroTag}) {
    return Navigator.of(context).push(
      MaterialPageRoute(
        builder: (_) => FullScreenImageViewer(imageUrl: imageUrl, heroTag: heroTag),
        fullscreenDialog: true,
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    return Scaffold(
      backgroundColor: Colors.black,
      appBar: AppBar(
        backgroundColor: Colors.black,
        iconTheme: const IconThemeData(color: Colors.white),
        elevation: 0,
      ),
      body: _buildImageView(),
    );
  }

  Widget _buildImageView() {
    // Check if it's a base64 data URI
    final bytes = PhotoUtil.bytesFromDataUri(imageUrl);
    if (bytes != null) {
      final imageProvider = MemoryImage(bytes);
      return _buildPhotoView(imageProvider);
    }

    // Check if it's a network URL
    final url = PhotoUtil.resolveNetworkUrl(imageUrl);
    if (url != null) {
      final imageProvider = CachedNetworkImageProvider(url);
      return _buildPhotoView(imageProvider);
    }

    // No valid image
    return const Center(
      child: Icon(Icons.broken_image_outlined, size: 64, color: Colors.white54),
    );
  }

  Widget _buildPhotoView(ImageProvider imageProvider) {
    if (heroTag != null) {
      return Hero(
        tag: heroTag!,
        child: PhotoView(
          imageProvider: imageProvider,
          backgroundDecoration: const BoxDecoration(color: Colors.black),
          minScale: PhotoViewComputedScale.contained,
          maxScale: PhotoViewComputedScale.covered * 3.0,
          initialScale: PhotoViewComputedScale.contained,
          heroAttributes: PhotoViewHeroAttributes(tag: heroTag!),
        ),
      );
    }

    return PhotoView(
      imageProvider: imageProvider,
      backgroundDecoration: const BoxDecoration(color: Colors.black),
      minScale: PhotoViewComputedScale.contained,
      maxScale: PhotoViewComputedScale.covered * 3.0,
      initialScale: PhotoViewComputedScale.contained,
    );
  }
}
