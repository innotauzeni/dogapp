import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/network/api_exception.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/image_crop_picker.dart';
import '../data/dogs_repository.dart';
import 'dog_avatar.dart';

/// Tappable circular photo picker. Picks from camera/gallery with cropping,
/// resizes & compresses, then uploads via the API (which stores it on Cloudflare R2) and
/// reports the resulting public URL back through [onChanged]. Photo is optional.
class PhotoPickerField extends StatefulWidget {
  /// Current value — an uploaded URL or null.
  final String? value;
  final ValueChanged<String?> onChanged;
  final String label;
  final bool enableCrop;

  const PhotoPickerField({
    super.key,
    required this.value,
    required this.onChanged,
    this.label = 'Photo',
    this.enableCrop = true,
  });

  @override
  State<PhotoPickerField> createState() => _PhotoPickerFieldState();
}

class _PhotoPickerFieldState extends State<PhotoPickerField> {
  final _repo = DogsRepository();
  bool _uploading = false;

  Future<void> _pickAndUpload() async {
    if (_uploading) return;
    
    // Show picker with crop option
    final imagePath = await ImageCropPicker.showImageSourcePicker(
      context,
      allowCrop: widget.enableCrop,
    );
    
    if (imagePath == null) return;
    
    setState(() => _uploading = true);
    try {
      final url = await _repo.uploadPhoto(imagePath);
      widget.onChanged(url);
    } on ApiException catch (e) {
      _toast(e.message);
    } catch (e) {
      _toast('Upload failed: $e');
    } finally {
      if (mounted) setState(() => _uploading = false);
    }
  }

  void _toast(String msg) {
    if (!mounted) return;
    ScaffoldMessenger.of(context).showSnackBar(
      SnackBar(backgroundColor: AppColors.danger, content: Text(msg)),
    );
  }

  Future<void> _showOptions() async {
    if (_uploading) return;
    final hasImage = widget.value != null && widget.value!.isNotEmpty;
    await showModalBottomSheet<void>(
      context: context,
      showDragHandle: true,
      builder: (sheetContext) => SafeArea(
        child: Column(
          mainAxisSize: MainAxisSize.min,
          children: [
            ListTile(
              leading: const Icon(Icons.photo_camera_outlined),
              title: Text(widget.enableCrop ? 'Take & Crop Photo' : 'Take Photo'),
              onTap: () {
                Navigator.pop(sheetContext);
                _pickAndUpload();
              },
            ),
            ListTile(
              leading: const Icon(Icons.photo_library_outlined),
              title: Text(widget.enableCrop ? 'Choose & Crop from Gallery' : 'Choose from Gallery'),
              onTap: () {
                Navigator.pop(sheetContext);
                _pickAndUpload();
              },
            ),
            if (hasImage)
              ListTile(
                leading: Icon(Icons.delete_outline_rounded, color: AppColors.danger),
                title: Text('Remove photo', style: TextStyle(color: AppColors.danger)),
                onTap: () {
                  Navigator.pop(sheetContext);
                  widget.onChanged(null);
                },
              ),
          ],
        ),
      ),
    );
  }

  @override
  Widget build(BuildContext context) {
    final hasImage = widget.value != null && widget.value!.isNotEmpty;
    return Center(
      child: Column(
        children: [
          GestureDetector(
            onTap: _showOptions,
            child: Stack(
              alignment: Alignment.center,
              children: [
                Container(
                  decoration: BoxDecoration(
                    shape: BoxShape.circle,
                    border: Border.all(color: AppColors.border, width: 2),
                  ),
                  child: DogAvatar(photoUrl: widget.value, size: 96),
                ),
                if (_uploading)
                  Container(
                    width: 96.r,
                    height: 96.r,
                    decoration: BoxDecoration(
                      color: Colors.black.withValues(alpha: 0.35),
                      shape: BoxShape.circle,
                    ),
                    child: const Center(
                      child: CircularProgressIndicator(strokeWidth: 2.6, color: Colors.white),
                    ),
                  ),
                if (!_uploading)
                  Positioned(
                    right: 0,
                    bottom: 0,
                    child: Container(
                      padding: EdgeInsets.all(6.r),
                      decoration: BoxDecoration(
                        color: AppColors.primary,
                        shape: BoxShape.circle,
                        border: Border.all(color: Colors.white, width: 2),
                      ),
                      child: Icon(Icons.camera_alt_rounded, color: Colors.white, size: 16.r),
                    ),
                  ),
              ],
            ),
          ),
          SizedBox(height: 8.h),
          Text(
            _uploading ? 'Uploading…' : (hasImage ? 'Change ${widget.label}' : 'Add ${widget.label}'),
            style: TextStyle(fontSize: 12.sp, color: AppColors.primary, fontWeight: FontWeight.w600),
          ),
        ],
      ),
    );
  }
}
