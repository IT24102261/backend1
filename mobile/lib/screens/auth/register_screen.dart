import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:image_picker/image_picker.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';

class RegisterScreen extends ConsumerStatefulWidget {
  const RegisterScreen({super.key, this.initialRole});

  final String? initialRole;

  @override
  ConsumerState<RegisterScreen> createState() => _RegisterScreenState();
}

class _RegisterScreenState extends ConsumerState<RegisterScreen> {
  final _form = GlobalKey<FormState>();
  final _name = TextEditingController();
  final _email = TextEditingController();
  final _phone = TextEditingController();
  final _address = TextEditingController();
  final _password = TextEditingController();
  late String _role = widget.initialRole == 'TECHNICIAN' ? 'TECHNICIAN' : 'CUSTOMER';
  String? _categoryId;
  List<Category> _categories = const [];
  XFile? _nicPhoto;
  XFile? _certificate;
  XFile? _profilePhoto;
  String? _error;
  bool _busy = false;

  bool get _isElectrician {
    final selected = _categories.where((item) => item.id == _categoryId);
    return selected.isNotEmpty && selected.first.name.toLowerCase() == 'electrician';
  }

  @override
  void initState() {
    super.initState();
    _loadCategories();
  }

  Future<void> _loadCategories() async {
    try {
      final items = await ref.read(apiProvider).categories();
      if (mounted) setState(() => _categories = items.where((item) => item.isActive).toList());
    } catch (_) {}
  }

  @override
  void dispose() {
    _name.dispose();
    _email.dispose();
    _phone.dispose();
    _address.dispose();
    _password.dispose();
    super.dispose();
  }

  Future<void> _pickProfile() async {
    final file = await ImagePicker().pickImage(source: ImageSource.gallery);
    if (file != null) setState(() => _profilePhoto = file);
  }

  Future<void> _pickNic() async {
    final file = await ImagePicker().pickImage(source: ImageSource.gallery);
    if (file != null) setState(() => _nicPhoto = file);
  }

  Future<void> _pickCertificate() async {
    final file = await ImagePicker().pickImage(source: ImageSource.gallery);
    if (file != null) setState(() => _certificate = file);
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    if (_role == 'TECHNICIAN' && _profilePhoto == null) {
      setState(() => _error = 'Upload a profile photo.');
      return;
    }
    if (_role == 'TECHNICIAN' && _nicPhoto == null) {
      setState(() => _error = 'Upload a photo of your NIC.');
      return;
    }
    if (_role == 'TECHNICIAN' && _isElectrician && _certificate == null) {
      setState(() => _error = 'Electricians must upload their studied certificate.');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final user = await ref.read(authProvider.notifier).register(
            email: _email.text.trim(),
            password: _password.text,
            displayName: _name.text.trim(),
            role: _role,
            phone: _phone.text.trim().isEmpty ? null : _phone.text.trim(),
            address: _role == 'TECHNICIAN' ? _address.text.trim() : null,
            categoryId: _role == 'TECHNICIAN' ? _categoryId : null,
            nicPhotoPath: _role == 'TECHNICIAN' ? _nicPhoto?.path : null,
            nicPhotoName: _role == 'TECHNICIAN' ? _nicPhoto?.name : null,
            certificatePath: _role == 'TECHNICIAN' ? _certificate?.path : null,
            certificateName: _role == 'TECHNICIAN' ? _certificate?.name : null,
            profilePhotoPath: _role == 'TECHNICIAN' ? _profilePhoto?.path : null,
            profilePhotoName: _role == 'TECHNICIAN' ? _profilePhoto?.name : null,
          );
      if (!mounted) return;
      if (user == null) {
        ScaffoldMessenger.of(context).showSnackBar(
          const SnackBar(content: Text('Registration received. Wait for admin approval before signing in.')),
        );
        Navigator.pushReplacementNamed(context, AppRoutes.login);
        return;
      }
      final route = user.isTechnician ? AppRoutes.technicianHome : AppRoutes.customerHome;
      Navigator.pushReplacementNamed(context, route);
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
        padding: const EdgeInsets.symmetric(vertical: 12),
        children: [
          AuthCard(
            child: Form(
              key: _form,
              child: Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  const BrandMark(),
                  const SizedBox(height: 16),
                  Text('Create an account', style: Theme.of(context).textTheme.headlineMedium),
                  const SizedBox(height: 8),
                  Text(
                    _role == 'TECHNICIAN'
                        ? 'Technician accounts are reviewed by an admin before login is allowed.'
                        : 'Create a customer account to request home services.',
                    style: const TextStyle(color: AppColors.muted, height: 1.5),
                  ),
                  if (_error != null) ...[
                    const SizedBox(height: 16),
                    ErrorView(message: _error!),
                  ],
                  const SizedBox(height: 20),
                DropdownButtonFormField<String>(
                  // ignore: deprecated_member_use
                  value: _role,
                  decoration: const InputDecoration(labelText: 'I am a'),
                  items: const [
                    DropdownMenuItem(value: 'CUSTOMER', child: Text('Customer')),
                    DropdownMenuItem(value: 'TECHNICIAN', child: Text('Technician')),
                  ],
                  onChanged: (value) => setState(() => _role = value ?? 'CUSTOMER'),
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _name,
                  decoration: const InputDecoration(labelText: 'Full name'),
                  validator: (value) => value == null || value.trim().length < 2 ? 'Enter your name' : null,
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _email,
                  decoration: const InputDecoration(labelText: 'Email'),
                  validator: (value) => value != null && value.contains('@') ? null : 'Enter a valid email',
                ),
                const SizedBox(height: 12),
                TextFormField(
                  controller: _phone,
                  decoration: const InputDecoration(labelText: 'Phone'),
                  validator: (value) => _role == 'TECHNICIAN' && (value == null || value.trim().isEmpty)
                      ? 'Enter your phone number'
                      : null,
                ),
                if (_role == 'TECHNICIAN') ...[
                  const SizedBox(height: 12),
                  TextFormField(
                    controller: _address,
                    decoration: const InputDecoration(labelText: 'Address'),
                    validator: (value) => value == null || value.trim().isEmpty ? 'Enter your address' : null,
                  ),
                  const SizedBox(height: 12),
                  DropdownButtonFormField<String>(
                    // ignore: deprecated_member_use
                    value: _categoryId,
                    decoration: const InputDecoration(labelText: 'Field you work in'),
                    items: _categories
                        .map((item) => DropdownMenuItem(value: item.id, child: Text(item.name)))
                        .toList(),
                    onChanged: (value) => setState(() => _categoryId = value),
                    validator: (value) => value == null || value.isEmpty ? 'Select your trade' : null,
                  ),
                  const SizedBox(height: 12),
                  OutlinedButton(
                    onPressed: _pickProfile,
                    child: Text(_profilePhoto == null ? 'Upload profile photo' : 'Profile photo: ${_profilePhoto!.name}'),
                  ),
                  const SizedBox(height: 8),
                  OutlinedButton(
                    onPressed: _pickNic,
                    child: Text(_nicPhoto == null ? 'Upload NIC photo' : 'NIC photo: ${_nicPhoto!.name}'),
                  ),
                  const SizedBox(height: 8),
                  OutlinedButton(
                    onPressed: _pickCertificate,
                    child: Text(
                      _certificate == null
                          ? (_isElectrician ? 'Upload studied certificate' : 'Upload studied certificate (optional)')
                          : 'Certificate: ${_certificate!.name}',
                    ),
                  ),
                ],
                const SizedBox(height: 12),
                TextFormField(
                  controller: _password,
                  obscureText: true,
                  decoration: const InputDecoration(labelText: 'Password'),
                  validator: (value) => value != null && value.length >= 8 ? null : 'Use at least 8 characters',
                ),
                const SizedBox(height: 20),
                FilledButton(onPressed: _busy ? null : _submit, child: Text(_busy ? 'Creating…' : 'Register')),
                const SizedBox(height: 12),
                Wrap(
                  children: [
                    const Text('Already registered? ', style: TextStyle(color: AppColors.muted)),
                    GestureDetector(
                      onTap: () => Navigator.pushNamed(context, AppRoutes.login),
                      child: const Text('Sign in', style: TextStyle(color: AppColors.gold, fontWeight: FontWeight.w700)),
                    ),
                    const Text(' · ', style: TextStyle(color: AppColors.muted)),
                    GestureDetector(
                      onTap: () => Navigator.pushNamedAndRemoveUntil(context, AppRoutes.home, (_) => false),
                      child: const Text('Home', style: TextStyle(color: AppColors.gold, fontWeight: FontWeight.w700)),
                    ),
                  ],
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
