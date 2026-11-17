import 'package:flutter/material.dart';

import '../../core/network/api_exception.dart';
import 'state_views.dart';

/// Renders loading / error / data states for a [Future]. Pair with
/// [RefreshIndicator] by calling [onRetry] from the parent's refresh callback.
class AsyncView<T> extends StatelessWidget {
  final Future<T>? future;
  final Widget Function(BuildContext, T) builder;
  final VoidCallback? onRetry;
  final String? loadingLabel;

  const AsyncView({
    super.key,
    required this.future,
    required this.builder,
    this.onRetry,
    this.loadingLabel,
  });

  @override
  Widget build(BuildContext context) {
    return FutureBuilder<T>(
      future: future,
      builder: (context, snapshot) {
        if (snapshot.connectionState == ConnectionState.waiting) {
          return AppLoader(label: loadingLabel);
        }
        if (snapshot.hasError) {
          final err = snapshot.error;
          final message = err is ApiException ? err.message : 'Could not load data.';
          return StatusView.error(message: message, onRetry: onRetry);
        }
        if (!snapshot.hasData) {
          return StatusView.empty(title: 'Nothing here yet');
        }
        return builder(context, snapshot.data as T);
      },
    );
  }
}
