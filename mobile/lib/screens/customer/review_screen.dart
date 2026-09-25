import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';

class ReviewScreen extends ConsumerStatefulWidget {
  const ReviewScreen({super.key, required this.booking});

  final Booking booking;

  @override
  ConsumerState<ReviewScreen> createState() => _ReviewScreenState();
}

class _ReviewScreenState extends ConsumerState<ReviewScreen> {
  int _rating = 5;
  final _body = TextEditingController();
  String? _error;
  bool _busy = false;
  bool _done = false;

  @override
  void dispose() {
    _body.dispose();
    super.dispose();
  }

  Future<void> _submit() async {
    if (_rating < 1 || _rating > 5) {
      setState(() => _error = 'Rating must be between 1 and 5.');
      return;
    }
    if (_body.text.trim().isEmpty) {
      setState(() => _error = 'Write a short review.');
      return;
    }
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final existing = await ref.read(apiProvider).technicianReviews(widget.booking.technicianId);
      if (existing.any((item) => item.bookingId == widget.booking.id)) {
        setState(() {
          _error = 'This booking already has a review.';
          _done = true;
        });
        return;
      }
      await ref.read(apiProvider).createReview(widget.booking.id, _rating, _body.text.trim());
      setState(() => _done = true);
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return FixFlowScaffold(
      title: 'Review',
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: _done
            ? const Text('Thank you. Duplicate reviews for this booking are blocked by the API.')
            : Column(
                crossAxisAlignment: CrossAxisAlignment.start,
                children: [
                  Text('Booking ${shortId(widget.booking.id)}'),
                  Text(
                    widget.booking.technicianDisplayName?.isNotEmpty == true
                        ? 'Technician: ${widget.booking.technicianDisplayName}'
                        : 'Rate the technician after you confirmed completion.',
                  ),
                  Row(
                    children: List.generate(
                      5,
                      (index) => IconButton(
                        onPressed: () => setState(() => _rating = index + 1),
                        icon: Icon(index < _rating ? Icons.star : Icons.star_border, color: Colors.amber),
                      ),
                    ),
                  ),
                  Text('$_rating / 5'),
                  TextField(controller: _body, maxLines: 4, decoration: const InputDecoration(labelText: 'Review text')),
                  if (_error != null) Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                  const SizedBox(height: 16),
                  FilledButton(onPressed: _busy ? null : _submit, child: Text(_busy ? 'Submitting…' : 'Submit')),
                ],
              ),
      ),
    );
  }
}
