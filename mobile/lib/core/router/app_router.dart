import 'package:go_router/go_router.dart';

import '../../features/auth/providers/auth_provider.dart';
import '../../features/auth/screens/login_screen.dart';
import '../../features/auth/screens/splash_screen.dart';
import '../../features/home/home_shell.dart';

/// Top-level navigation. Auth gating is handled by [redirect]; feature detail
/// pages are pushed with `Navigator.push` from within each module.
class AppRouter {
  AppRouter._();

  static GoRouter build(AuthProvider auth) {
    return GoRouter(
      initialLocation: '/splash',
      refreshListenable: auth,
      routes: [
        GoRoute(path: '/splash', builder: (context, state) => const SplashScreen()),
        GoRoute(path: '/login', builder: (context, state) => const LoginScreen()),
        GoRoute(path: '/home', builder: (context, state) => const HomeShell()),
      ],
      redirect: (context, state) {
        final loggingIn = state.matchedLocation == '/login';
        final onSplash = state.matchedLocation == '/splash';

        switch (auth.status) {
          case AuthStatus.unknown:
            return onSplash ? null : '/splash';
          case AuthStatus.unauthenticated:
            return loggingIn ? null : '/login';
          case AuthStatus.authenticated:
            return (loggingIn || onSplash) ? '/home' : null;
        }
      },
    );
  }
}
