import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';

class CreateQuoteScreen extends ConsumerStatefulWidget {
  const CreateQuoteScreen({super.key, required this.invitation});

  final Invitation invitation;

  @override
  ConsumerState<CreateQuoteScreen> createState() => _CreateQuoteScreenState();
}

class _CreateQuoteScreenState extends ConsumerState<CreateQuoteScreen> {
  final _form = GlobalKey<FormState>();
  final _labour = TextEditingController(text: '0');
  final _materials = TextEditingController(text: '0');
  final _travel = TextEditingController(text: '0');
  final _assumptions = TextEditingController();
  final _currency = TextEditingController(text: 'LKR');
  DateTime? _arrival;
  bool _busy = false;
  String? _error;

  num get _total =>
      (num.tryParse(_labour.text) ?? 0) + (num.tryParse(_materials.text) ?? 0) + (num.tryParse(_travel.text) ?? 0);

  @override
  void dispose() {
    _labour.dispose();
    _materials.dispose();
    _travel.dispose();
    _assumptions.dispose();
    _currency.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (!_form.currentState!.validate()) return;
    if (_arrival == null) {
      setState(() => _error = 'Set an arrival window.');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      await ref.read(apiProvider).createQuote(widget.invitation.id, {
        'labourAmount': num.parse(_labour.text),
        'materialsAmount': num.parse(_materials.text),
        'travelAmount': num.parse(_travel.text),
        'totalAmount': _total,
        'currency': _currency.text.trim().toUpperCase(),
        'arrivalStart': _arrival!.toUtc().toIso8601String(),
        'assumptions': _assumptions.text.trim(),
      });
      if (!mounted) return;
      Navigator.pop(context);
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return FixFlowScaffold(
      title: 'Create quotation',
      body: Form(
        key: _form,
        child: ListView(
          padding: const EdgeInsets.all(16),
          children: [
            if (_error != null) Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
            TextFormField(
              controller: _labour,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Labour estimate'),
              onChanged: (_) => setState(() {}),
              validator: (value) => num.tryParse(value ?? '') == null ? 'Enter an amount' : null,
            ),
            TextFormField(
              controller: _materials,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Materials estimate'),
              onChanged: (_) => setState(() {}),
            ),
            TextFormField(
              controller: _travel,
              keyboardType: TextInputType.number,
              decoration: const InputDecoration(labelText: 'Travel'),
              onChanged: (_) => setState(() {}),
            ),
            const SizedBox(height: 8),
            Text('Visual total: ${formatMoney(_total, _currency.text.trim().isEmpty ? 'LKR' : _currency.text.trim().toUpperCase())} — the API still validates labour + materials + travel.'),
            TextFormField(
              controller: _currency,
              decoration: const InputDecoration(labelText: 'Currency'),
              textCapitalization: TextCapitalization.characters,
              onChanged: (_) => setState(() {}),
              validator: (value) => value != null && value.trim().length == 3 ? null : 'Use a 3-letter currency',
            ),
            TextFormField(controller: _assumptions, maxLines: 3, decoration: const InputDecoration(labelText: 'Assumptions')),
            ListTile(
              title: Text(_arrival == null ? 'Arrival' : formatDate(_arrival)),
              onTap: () async {
                final date = await showDatePicker(
                  context: context,
                  firstDate: DateTime.now(),
                  lastDate: DateTime.now().add(const Duration(days: 30)),
                  initialDate: DateTime.now(),
                );
                if (date == null || !context.mounted) return;
                final time = await showTimePicker(context: context, initialTime: TimeOfDay.now());
                if (time == null) return;
                setState(() => _arrival = DateTime(date.year, date.month, date.day, time.hour, time.minute));
              },
            ),
            if (!widget.invitation.canQuote)
              Padding(
                padding: const EdgeInsets.only(bottom: 12),
                child: Text(
                  'This job has already been accepted by another technician. Quotations are no longer accepted.',
                  style: TextStyle(color: Theme.of(context).colorScheme.error),
                ),
              ),
            FilledButton(
              onPressed: _busy || !widget.invitation.canQuote ? null : _submit,
              child: Text(_busy ? 'Submitting…' : 'Submit quote'),
            ),
          ],
        ),
      ),
    );
  }
}
