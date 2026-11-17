import 'package:flutter/material.dart';
import 'package:flutter/services.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import 'core/config/app_config.dart';
import 'core/responsive/responsive.dart';
import 'core/router/app_router.dart';
import 'core/theme/app_theme.dart';
import 'features/auth/providers/auth_provider.dart';

void main() {
  WidgetsFlutterBinding.ensureInitialized();
  SystemChrome.setPreferredOrientations([
    DeviceOrientation.portraitUp,
    DeviceOrientation.portraitDown,
  ]);
  runApp(const BlackEagleApp());
}

class BlackEagleApp extends StatelessWidget {
  const BlackEagleApp({super.key});

  @override
  Widget build(BuildContext context) {
    return ChangeNotifierProvider(
      create: (_) => AuthProvider()..bootstrap(),
      child: Consumer<AuthProvider>(
        builder: (context, auth, _) {
          // ScreenUtilInit makes `.w / .h / .sp / .r` resolve against the real
          // device size, so the same code fits every Android screen.
          return ScreenUtilInit(
            designSize: kDesignSize,
            minTextAdapt: true,
            splitScreenMode: true,
            builder: (context, _) {
              final router = AppRouter.build(auth);
              return MaterialApp.router(
                title: AppConfig.appName,
                debugShowCheckedModeBanner: false,
                theme: AppTheme.light(seed: auth.brandColor),
                routerConfig: router,
                builder: (context, child) {
                  // Clamp the OS text-scale so very large accessibility settings
                  // never break our proportional layout.
                  final mq = MediaQuery.of(context);
                  final clamped = mq.textScaler.clamp(minScaleFactor: 0.85, maxScaleFactor: 1.3);
                  return MediaQuery(data: mq.copyWith(textScaler: clamped), child: child!);
                },
              );
            },
          );
        },
      ),
    );
  }
}
