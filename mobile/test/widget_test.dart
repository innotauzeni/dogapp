// Basic smoke test for the Black Eagle app shell.
import 'package:flutter_test/flutter_test.dart';

import 'package:black_eagle/main.dart';

void main() {
  testWidgets('App boots to the splash screen', (WidgetTester tester) async {
    await tester.pumpWidget(const BlackEagleApp());
    // The first frame shows the branded splash while the session is restored.
    expect(find.text('SURVEILLANCE'), findsOneWidget);
  });
}
