import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/status_chip.dart';
import '../models/animal.dart';
import 'dog_avatar.dart';

/// Row used in the dogs list & pickers.
class DogListTile extends StatelessWidget {
  final Animal dog;
  final VoidCallback? onTap;
  const DogListTile({super.key, required this.dog, this.onTap});

  @override
  Widget build(BuildContext context) {
    return AppCard(
      onTap: onTap,
      padding: EdgeInsets.all(12.w),
      child: Row(
        children: [
          DogAvatar(photoUrl: dog.photoUrl, size: 52),
          SizedBox(width: 12.w),
          Expanded(
            child: Column(
              crossAxisAlignment: CrossAxisAlignment.start,
              children: [
                Row(
                  children: [
                    Expanded(
                      child: Text(
                        dog.name,
                        maxLines: 1,
                        overflow: TextOverflow.ellipsis,
                        style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w700, color: AppColors.textPrimary),
                      ),
                    ),
                    SizedBox(width: 8.w),
                    if (dog.isDeceased)
                      StatusChip.forStatus('DECEASED')
                    else
                      StatusChip.boolStatus(dog.isActive),
                  ],
                ),
                SizedBox(height: 4.h),
                Text(
                  '${dog.breed.isEmpty ? 'Unknown breed' : dog.breed} • ${Fmt.age(dog.dateOfBirth)}',
                  maxLines: 1,
                  overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary),
                ),
                SizedBox(height: 6.h),
                Row(
                  children: [
                    _meta(Icons.male_rounded, dog.gender ?? '—'),
                    SizedBox(width: 12.w),
                    if (dog.isDeployed)
                      Expanded(
                        child: _meta(
                          Icons.location_on_rounded,
                          dog.deployedLocation ?? 'Deployed',
                          color: AppColors.info,
                        ),
                      ),
                  ],
                ),
              ],
            ),
          ),
          Icon(Icons.chevron_right_rounded, color: AppColors.textMuted, size: 22.r),
        ],
      ),
    );
  }

  Widget _meta(IconData icon, String text, {Color? color}) {
    return Row(
      mainAxisSize: MainAxisSize.min,
      children: [
        Icon(icon, size: 14.r, color: color ?? AppColors.textMuted),
        SizedBox(width: 4.w),
        Flexible(
          child: Text(
            text,
            maxLines: 1,
            overflow: TextOverflow.ellipsis,
            style: TextStyle(fontSize: 11.sp, color: color ?? AppColors.textSecondary),
          ),
        ),
      ],
    );
  }
}
