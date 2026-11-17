import 'package:cached_network_image/cached_network_image.dart';
import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/photo_util.dart';
import '../../../shared/widgets/full_screen_image_viewer.dart';

/// Circular dog photo. Handles base64 data URIs (uploaded from mobile),
/// absolute/portal-relative network URLs, and a graceful icon fallback.
class DogAvatar extends StatelessWidget {
  final String? photoUrl;
  final double size;
  final bool enableFullScreenView;
  final String? heroTag;
  
  const DogAvatar({
    super.key, 
    this.photoUrl, 
    this.size = 52,
    this.enableFullScreenView = false,
    this.heroTag,
  });

  @override
  Widget build(BuildContext context) {
    final dimension = size.r;
    final placeholder = Container(
      width: dimension,
      height: dimension,
      decoration: const BoxDecoration(color: AppColors.primaryLight, shape: BoxShape.circle),
      child: Icon(Icons.pets_rounded, color: AppColors.primary, size: dimension * 0.5),
    );

    Widget avatarWidget;
    
    final bytes = PhotoUtil.bytesFromDataUri(photoUrl);
    if (bytes != null) {
      avatarWidget = ClipOval(
        child: Image.memory(bytes, width: dimension, height: dimension, fit: BoxFit.cover),
      );
    } else {
      final url = PhotoUtil.resolveNetworkUrl(photoUrl);
      if (url == null) {
        avatarWidget = placeholder;
      } else {
        avatarWidget = ClipOval(
          child: CachedNetworkImage(
            imageUrl: url,
            width: dimension,
            height: dimension,
            fit: BoxFit.cover,
            placeholder: (context, url) => placeholder,
            errorWidget: (context, url, error) => placeholder,
          ),
        );
      }
    }

    // Wrap with Hero if heroTag is provided
    if (heroTag != null && photoUrl != null && photoUrl!.isNotEmpty) {
      avatarWidget = Hero(tag: heroTag!, child: avatarWidget);
    }

    // Make it clickable if enableFullScreenView is true and there's a valid image
    if (enableFullScreenView && photoUrl != null && photoUrl!.isNotEmpty) {
      return GestureDetector(
        onTap: () => FullScreenImageViewer.show(context, photoUrl, heroTag: heroTag),
        child: avatarWidget,
      );
    }

    return avatarWidget;
  }
}
