import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/config/app_config.dart';
import '../../../core/theme/app_colors.dart';
import '../../../shared/widgets/brand_logo.dart';
import '../../../shared/widgets/inputs.dart';
import '../providers/auth_provider.dart';

class LoginScreen extends StatefulWidget {
  const LoginScreen({super.key});

  @override
  State<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends State<LoginScreen> {
  final _formKey = GlobalKey<FormState>();
  final _identifier = TextEditingController();
  final _password = TextEditingController();
  bool _obscure = true;

  @override
  void dispose() {
    _identifier.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    FocusScope.of(context).unfocus();
    if (!_formKey.currentState!.validate()) return;
    final auth = context.read<AuthProvider>();
    final ok = await auth.login(_identifier.text.trim(), _password.text);
    if (!ok && mounted) {
      ScaffoldMessenger.of(context).showSnackBar(
        SnackBar(
          backgroundColor: AppColors.danger,
          content: Text(auth.error ?? 'Login failed'),
        ),
      );
    }
    // On success the router redirect moves us to /home automatically.
  }

  @override
  Widget build(BuildContext context) {
    final auth = context.watch<AuthProvider>();

    return Scaffold(
      backgroundColor: AppColors.surface,
      body: SafeArea(
        child: LayoutBuilder(
          builder: (context, constraints) {
            return SingleChildScrollView(
              // Fill the viewport so vertical centering looks right on big
              // phones while still scrolling when the keyboard appears.
              child: ConstrainedBox(
                constraints: BoxConstraints(minHeight: constraints.maxHeight),
                child: Center(
                  child: ConstrainedBox(
                    // Cap width so the form doesn't stretch awkwardly on tablets.
                    constraints: BoxConstraints(maxWidth: 460.w),
                    child: Padding(
                      padding: EdgeInsets.symmetric(horizontal: 24.w, vertical: 16.h),
                      child: Column(
                        mainAxisSize: MainAxisSize.min,
                        crossAxisAlignment: CrossAxisAlignment.stretch,
                        children: [
                          SizedBox(height: 12.h),
                          Center(child: const BrandLogo(size: 84)),
                          SizedBox(height: 20.h),
                          Text(
                            'Welcome back',
                            textAlign: TextAlign.center,
                            style: Theme.of(context).textTheme.headlineMedium,
                          ),
                          SizedBox(height: 6.h),
                          Text(
                            'Sign in to ${AppConfig.appName} ${AppConfig.appTagline}',
                            textAlign: TextAlign.center,
                            style: Theme.of(context).textTheme.bodyMedium,
                          ),
                          SizedBox(height: 32.h),
                          Form(
                            key: _formKey,
                            child: Column(
                              crossAxisAlignment: CrossAxisAlignment.stretch,
                              children: [
                                AppTextField(
                                  controller: _identifier,
                                  label: 'Email, username or phone',
                                  hint: 'you@example.com',
                                  icon: Icons.person_outline_rounded,
                                  keyboardType: TextInputType.emailAddress,
                                  textInputAction: TextInputAction.next,
                                  validator: (v) =>
                                      (v == null || v.trim().isEmpty) ? 'Enter your credentials' : null,
                                ),
                                SizedBox(height: 18.h),
                                AppTextField(
                                  controller: _password,
                                  label: 'Password',
                                  hint: '••••••••',
                                  icon: Icons.lock_outline_rounded,
                                  obscure: _obscure,
                                  textInputAction: TextInputAction.done,
                                  validator: (v) =>
                                      (v == null || v.isEmpty) ? 'Enter your password' : null,
                                  suffix: IconButton(
                                    icon: Icon(
                                      _obscure ? Icons.visibility_off_rounded : Icons.visibility_rounded,
                                      size: 20.r,
                                      color: AppColors.textMuted,
                                    ),
                                    onPressed: () => setState(() => _obscure = !_obscure),
                                  ),
                                ),
                                SizedBox(height: 28.h),
                                PrimaryButton(
                                  label: 'Sign In',
                                  loading: auth.busy,
                                  onPressed: _submit,
                                ),
                              ],
                            ),
                          ),
                          SizedBox(height: 24.h),
                          Text(
                            '© ${DateTime.now().year} ${AppConfig.appName} ${AppConfig.appTagline}',
                            textAlign: TextAlign.center,
                            style: Theme.of(context).textTheme.bodySmall,
                          ),
                        ],
                      ),
                    ),
                  ),
                ),
              ),
            );
          },
        ),
      ),
    );
  }
}
