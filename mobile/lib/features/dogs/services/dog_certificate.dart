import 'dart:typed_data';

import 'package:dio/dio.dart';
import 'package:flutter/services.dart' show rootBundle;
import 'package:pdf/pdf.dart';
import 'package:pdf/widgets.dart' as pw;
import 'package:printing/printing.dart';

import '../../../core/utils/formatters.dart';
import '../../../core/utils/photo_util.dart';
import '../models/animal.dart';

/// Builds a branded "Certificate of Registration" PDF for a dog and opens the
/// native print/share/save sheet.
class DogCertificate {
  DogCertificate._();

  static final PdfColor _blue = PdfColor.fromInt(0xFF1D4ED8);
  static final PdfColor _blueDark = PdfColor.fromInt(0xFF1E3A8A);
  static final PdfColor _ink = PdfColor.fromInt(0xFF0F172A);
  static final PdfColor _muted = PdfColor.fromInt(0xFF64748B);
  static final PdfColor _line = PdfColor.fromInt(0xFFCBD5E1);

  /// Generates the certificate and presents the print/share preview.
  static Future<void> generate(Animal dog, {String? companyName, String? companyLogoUrl}) async {
    final bytes = await build(dog, companyName: companyName, companyLogoUrl: companyLogoUrl);
    await Printing.layoutPdf(
      name: 'Certificate - ${dog.name}.pdf',
      onLayout: (_) async => bytes,
    );
  }

  /// Builds the PDF document bytes (kept separate so it can be unit-tested).
  static Future<Uint8List> build(Animal dog, {String? companyName, String? companyLogoUrl}) async {
    final doc = pw.Document();

    // Try to load company logo first, fall back to app logo
    pw.ImageProvider logo;
    if (companyLogoUrl != null) {
      try {
        final logoBytes = await _loadImageFromUrl(companyLogoUrl);
        if (logoBytes != null) {
          logo = pw.MemoryImage(logoBytes);
        } else {
          logo = pw.MemoryImage((await rootBundle.load('assets/logo_mark.png')).buffer.asUint8List());
        }
      } catch (_) {
        logo = pw.MemoryImage((await rootBundle.load('assets/logo_mark.png')).buffer.asUint8List());
      }
    } else {
      logo = pw.MemoryImage((await rootBundle.load('assets/logo_mark.png')).buffer.asUint8List());
    }
    final photo = await _loadPhoto(dog.photoUrl);

    doc.addPage(
      pw.Page(
        pageFormat: PdfPageFormat.a4,
        margin: const pw.EdgeInsets.all(24),
        build: (context) => _frame(
          child: pw.Column(
            crossAxisAlignment: pw.CrossAxisAlignment.center,
            children: [
              _header(logo, companyName),
              pw.SizedBox(height: 18),
              pw.Text('CERTIFICATE OF REGISTRATION',
                  style: pw.TextStyle(fontSize: 22, fontWeight: pw.FontWeight.bold, color: _blueDark, letterSpacing: 2)),
              pw.SizedBox(height: 4),
              pw.Container(width: 120, height: 2, color: _blue),
              pw.SizedBox(height: 16),
              pw.Text('This is to certify that the dog described below is duly registered in the records of '
                  '${companyName ?? 'Black Eagle Surveillance'}.',
                  textAlign: pw.TextAlign.center,
                  style: pw.TextStyle(fontSize: 11, color: _muted)),
              pw.SizedBox(height: 18),
              if (photo != null) ...[
                pw.Container(
                  width: 110,
                  height: 110,
                  decoration: pw.BoxDecoration(
                    shape: pw.BoxShape.circle,
                    border: pw.Border.all(color: _blue, width: 2),
                    image: pw.DecorationImage(image: photo, fit: pw.BoxFit.cover),
                  ),
                ),
                pw.SizedBox(height: 12),
              ],
              pw.Text(dog.name,
                  style: pw.TextStyle(fontSize: 30, fontWeight: pw.FontWeight.bold, color: _ink)),
              pw.SizedBox(height: 2),
              pw.Text(dog.breed.isEmpty ? 'Unknown breed' : dog.breed,
                  style: pw.TextStyle(fontSize: 13, color: _blue, fontWeight: pw.FontWeight.bold)),
              pw.SizedBox(height: 20),
              _detailsTable(dog),
              pw.SizedBox(height: 16),
              _pedigree(dog),
              pw.Spacer(),
              _footer(),
            ],
          ),
        ),
      ),
    );

    return doc.save();
  }

  // ── Sections ───────────────────────────────────────────────────────────────

  static pw.Widget _frame({required pw.Widget child}) {
    return pw.Container(
      decoration: pw.BoxDecoration(border: pw.Border.all(color: _blue, width: 3)),
      padding: const pw.EdgeInsets.all(6),
      child: pw.Container(
        decoration: pw.BoxDecoration(border: pw.Border.all(color: _line, width: 1)),
        padding: const pw.EdgeInsets.symmetric(horizontal: 28, vertical: 26),
        child: child,
      ),
    );
  }

  static pw.Widget _header(pw.ImageProvider logo, String? companyName) {
    return pw.Column(
      children: [
        pw.SizedBox(height: 60, width: 60, child: pw.Image(logo)),
        pw.SizedBox(height: 8),
        pw.Text((companyName ?? 'BLACK EAGLE SURVEILLANCE').toUpperCase(),
            style: pw.TextStyle(fontSize: 16, fontWeight: pw.FontWeight.bold, color: _blueDark, letterSpacing: 1)),
        pw.Text('Security Company',
            style: pw.TextStyle(fontSize: 10, color: _muted, letterSpacing: 3)),
      ],
    );
  }

  static pw.Widget _detailsTable(Animal dog) {
    final rows = <List<String>>[
      ['Sex', dog.gender ?? '—', 'Colour', dog.color.isEmpty ? '—' : dog.color],
      ['Date of Birth', Fmt.date(dog.dateOfBirth), 'Age', Fmt.age(dog.dateOfBirth, until: dog.dateOfDeath)],
      ['Microchip No.', dog.microchipNumber ?? '—', 'Registration No.', _regNo(dog)],
    ];
    return pw.Table(
      border: pw.TableBorder.all(color: _line, width: 0.8),
      columnWidths: {
        0: const pw.FractionColumnWidth(0.22),
        1: const pw.FractionColumnWidth(0.28),
        2: const pw.FractionColumnWidth(0.22),
        3: const pw.FractionColumnWidth(0.28),
      },
      children: [
        for (final r in rows)
          pw.TableRow(children: [
            _cellLabel(r[0]),
            _cellValue(r[1]),
            _cellLabel(r[2]),
            _cellValue(r[3]),
          ]),
      ],
    );
  }

  static pw.Widget _pedigree(Animal dog) {
    pw.Widget parent(String role, String? name) => pw.Expanded(
          child: pw.Container(
            padding: const pw.EdgeInsets.all(10),
            margin: const pw.EdgeInsets.symmetric(horizontal: 4),
            decoration: pw.BoxDecoration(
              color: PdfColor.fromInt(0xFFF1F5F9),
              borderRadius: pw.BorderRadius.circular(6),
            ),
            child: pw.Column(
              crossAxisAlignment: pw.CrossAxisAlignment.start,
              children: [
                pw.Text(role, style: pw.TextStyle(fontSize: 9, color: _muted, letterSpacing: 1)),
                pw.SizedBox(height: 2),
                pw.Text(name == null || name.isEmpty ? 'Not recorded' : name,
                    style: pw.TextStyle(fontSize: 12, fontWeight: pw.FontWeight.bold, color: _ink)),
              ],
            ),
          ),
        );
    return pw.Column(
      crossAxisAlignment: pw.CrossAxisAlignment.start,
      children: [
        pw.Text('PEDIGREE', style: pw.TextStyle(fontSize: 10, color: _muted, letterSpacing: 2)),
        pw.SizedBox(height: 6),
        pw.Row(children: [parent('SIRE (Father)', dog.sireName), parent('DAM (Mother)', dog.damName)]),
      ],
    );
  }

  static pw.Widget _footer() {
    return pw.Row(
      mainAxisAlignment: pw.MainAxisAlignment.spaceBetween,
      crossAxisAlignment: pw.CrossAxisAlignment.end,
      children: [
        pw.Column(crossAxisAlignment: pw.CrossAxisAlignment.start, children: [
          pw.Text('Issued on', style: pw.TextStyle(fontSize: 9, color: _muted)),
          pw.Text(Fmt.date(DateTime.now()),
              style: pw.TextStyle(fontSize: 12, fontWeight: pw.FontWeight.bold, color: _ink)),
        ]),
        pw.Column(crossAxisAlignment: pw.CrossAxisAlignment.center, children: [
          pw.Container(width: 160, height: 0.8, color: _ink),
          pw.SizedBox(height: 4),
          pw.Text('Authorised Signature', style: pw.TextStyle(fontSize: 10, color: _muted)),
        ]),
      ],
    );
  }

  // ── Helpers ─────────────────────────────────────────────────────────────────

  static pw.Widget _cellLabel(String text) => pw.Container(
        color: PdfColor.fromInt(0xFFF8FAFC),
        padding: const pw.EdgeInsets.symmetric(horizontal: 8, vertical: 7),
        child: pw.Text(text, style: pw.TextStyle(fontSize: 10, color: _muted)),
      );

  static pw.Widget _cellValue(String text) => pw.Padding(
        padding: const pw.EdgeInsets.symmetric(horizontal: 8, vertical: 7),
        child: pw.Text(text, style: pw.TextStyle(fontSize: 11, fontWeight: pw.FontWeight.bold, color: _ink)),
      );

  static String _regNo(Animal dog) => 'BES-${dog.companyId}-${dog.id.toString().padLeft(5, '0')}';

  static Future<pw.ImageProvider?> _loadPhoto(String? url) async {
    try {
      final bytes = PhotoUtil.bytesFromDataUri(url);
      if (bytes != null) return pw.MemoryImage(bytes);
      final resolved = PhotoUtil.resolveNetworkUrl(url);
      if (resolved != null) return await networkImage(resolved);
    } catch (_) {/* skip photo on any failure */}
    return null;
  }

  static Future<Uint8List?> _loadImageFromUrl(String url) async {
    try {
      // Try data URI first
      final bytes = PhotoUtil.bytesFromDataUri(url);
      if (bytes != null) return bytes;

      // Try network URL
      final resolved = PhotoUtil.resolveNetworkUrl(url);
      if (resolved != null) {
        final dio = Dio();
        final response = await dio.get<List<int>>(resolved, options: Options(responseType: ResponseType.bytes));
        if (response.statusCode == 200 && response.data != null) return Uint8List.fromList(response.data!);
      }
    } catch (_) {/* skip on any failure */}
    return null;
  }
}
