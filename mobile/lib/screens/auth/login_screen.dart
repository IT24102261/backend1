import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/utils/constants.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/utils/validators.dart';

class LoginScreen extends ConsumerStatefulWidget {
  const LoginScreen({super.key});

  @override
  ConsumerState<LoginScreen> createState() => _LoginScreenState();
}

class _LoginScreenState extends ConsumerState<LoginScreen> {
  final _form = GlobalKey<FormState>();
  final _email = TextEditingController();
  final _password = TextEditingController();
  String? _error;
  bool _busy = false;
  bool _showPassword = false;

  @override
  void dispose() {
    _email.dispose();
    _password.dispose();
    super.dispose();
  }

  String _home(UserProfile user) {
    if (user.isTechnician) return AppRoutes.technicianHome;
    if (user.isCustomer) return AppRoutes.customerHome;
    if (user.isAdmin) return AppRoutes.adminHome;
    return AppRoutes.unauthorized;
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final user = await ref.read(authProvider.notifier).login(_email.text.trim(), _password.text);
      if (!mounted) return;
      Navigator.pushReplacementNamed(context, _home(user));
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return AuthShell(
      child: ListView(
        padding: const EdgeInsets.symmetric(vertical: 24),
        children: [
          AuthCard(
            child: Form(
              key: _form,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const BrandMark(),
                  const SizedBox(height: 16),
                  Text('Sign in', style: Theme.of(context).textTheme.headlineMedium),
                  const SizedBox(height: 8),
                  const Text('Use your FixFlow account. Authorization is always enforced by the API.', style: TextStyle(color: AppColors.muted, height: 1.5)),
                  if (_error != null) ...[
                    const SizedBox(height: 16),
                    ErrorView(message: _error!),
                  ],
                  const SizedBox(height: 24),
                  TextFormField(
                    controller: _email,
                    keyboardType: TextInputType.emailAddress,
                    decoration: const InputDecoration(labelText: 'Email'),
                    validator: emailValidator,
                  ),
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _password,
                    obscureText: !_showPassword,
                    decoration: InputDecoration(
                      labelText: 'Password',
                      suffixIcon: IconButton(
                        tooltip: _showPassword ? 'Hide password' : 'Show password',
                        onPressed: () => setState(() => _showPassword = !_showPassword),
                        icon: Icon(_showPassword ? Icons.visibility_off_outlined : Icons.visibility_outlined),
                      ),
                    ),
                    validator: (value) => value == null || value.isEmpty ? 'Password is required' : null,
                  ),
                  const SizedBox(height: 20),
                  FilledButton(onPressed: _busy ? null : _submit, child: Text(_busy ? 'Signing in…' : 'Sign in')),
                  const SizedBox(height: 12),
                  Wrap(
                    children: [
                      const Text('New here? ', style: TextStyle(color: AppColors.muted)),
                    GestureDetector(
                      onTap: () => Navigator.pushNamed(context, AppRoutes.register),
                      child: const Text('Create an account', style: TextStyle(color: AppColors.gold, fontWeight: FontWeight.w700)),
                    ),
                    const Text(' · ', style: TextStyle(color: AppColors.muted)),
                    GestureDetector(
                      onTap: () => Navigator.pushNamedAndRemoveUntil(context, AppRoutes.home, (_) => false),
                      child: const Text('Home', style: TextStyle(color: AppColors.gold, fontWeight: FontWeight.w700)),
                    ),
                    ],
                  ),
                  const SizedBox(height: 16),
                  Text(
                    'Connect to the same Wi-Fi as the PC.\nAPI ${AppConstants.apiBaseUrl}',
                    style: const TextStyle(fontSize: 11, color: AppColors.faint, height: 1.4),
                  ),
                ],
              ),
            ),
          ),
        ],
      ),
    );
  }
}
