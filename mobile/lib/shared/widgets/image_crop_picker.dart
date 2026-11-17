import 'dart:io';
import 'package:flutter/material.dart';
import 'package:image_cropper/image_cropper.dart';
import 'package:image_picker/image_picker.dart';

import '../../core/theme/app_colors.dart';

/// Image picker with cropping functionality for dog profile pictures
class ImageCropPicker {
  static Future<String?> pickAndCropImage(
    BuildContext context, {
    ImageSource source = ImageSource.gallery,
    CropAspectRatio aspectRatio = const CropAspectRatio(ratioX: 1, ratioY: 1),
    int maxWidth = 1400,
  }) async {
    try {
      // Step 1: Pick image
      final picker = ImagePicker();
      final pickedFile = await picker.pickImage(
        source: source,
        maxWidth: maxWidth.toDouble(),
        imageQuality: 85,
      );

      if (pickedFile == null) return null;

      // Step 2: Crop image
      final croppedFile = await ImageCropper().cropImage(
        sourcePath: pickedFile.path,
        aspectRatio: aspectRatio,
        uiSettings: [
          AndroidUiSettings(
            toolbarTitle: 'Crop Photo',
            toolbarColor: AppColors.primary,
            toolbarWidgetColor: Colors.white,
            initAspectRatio: CropAspectRatioPreset.square,
            lockAspectRatio: true,
            hideBottomControls: false,
            showCropGrid: true,
            cropGridColor: Colors.white,
            cropFrameColor: AppColors.primary,
            activeControlsWidgetColor: AppColors.primary,
          ),
          IOSUiSettings(
            title: 'Crop Photo',
            aspectRatioLockEnabled: true,
            resetAspectRatioEnabled: false,
            aspectRatioPickerButtonHidden: true,
            rotateButtonsHidden: false,
            minimumAspectRatio: 1.0,
          ),
        ],
      );

      if (croppedFile == null) return null;

      return croppedFile.path;
    } catch (e) {
      debugPrint('Error picking/cropping image: $e');
      if (context.mounted) {
        ScaffoldMessenger.of(context).showSnackBar(
          SnackBar(content: Text('Error: $e')),
        );
      }
      return null;
    }
  }

  /// Show bottom sheet to choose between camera and gallery
  static Future<String?> showImageSourcePicker(
    BuildContext context, {
    bool allowCrop = true,
  }) async {
    final source = await showModalBottomSheet<ImageSource>(
      context: context,
      shape: const RoundedRectangleBorder(
        borderRadius: BorderRadius.vertical(top: Radius.circular(18)),
      ),
      builder: (context) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            const SizedBox(height: 16),
            Text(
              'Choose Photo Source',
              style: Theme.of(context).textTheme.titleMedium?.copyWith(
                    fontWeight: FontWeight.bold,
                  ),
            ),
            const SizedBox(height: 16),
            ListTile(
              leading: const Icon(Icons.photo_camera),
              title: const Text('Camera'),
              onTap: () => Navigator.pop(context, ImageSource.camera),
            ),
            ListTile(
              leading: const Icon(Icons.photo_library),
              title: const Text('Gallery'),
              onTap: () => Navigator.pop(context, ImageSource.gallery),
            ),
            const SizedBox(height: 8),
            TextButton(
              onPressed: () => Navigator.pop(context),
              child: const Text('Cancel'),
            ),
            const SizedBox(height: 16),
          ],
        ),
      ),
    );

    if (source == null) return null;

    if (allowCrop) {
      return await pickAndCropImage(context, source: source);
    } else {
      // Just pick without cropping
      final picker = ImagePicker();
      final pickedFile = await picker.pickImage(
        source: source,
        maxWidth: 1400,
        imageQuality: 85,
      );
      return pickedFile?.path;
    }
  }
}
