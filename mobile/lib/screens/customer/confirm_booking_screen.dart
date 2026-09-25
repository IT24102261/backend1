import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';

class ConfirmBookingScreen extends ConsumerStatefulWidget {
  const ConfirmBookingScreen({super.key, required this.request, required this.quote});

  final ServiceRequest request;
  final Quote quote;

  @override
  ConsumerState<ConfirmBookingScreen> createState() => _ConfirmBookingScreenState();
}

class _ConfirmBookingScreenState extends ConsumerState<ConfirmBookingScreen> {
  bool _busy = false;
  String? _error;

  Future<void> _confirm() async {
    setState(() {
      _busy = true;
      _error = null;
    });
    try {
      final api = ref.read(apiProvider);
      var booking = await api.selectQuote(widget.quote.id);
      booking = await api.confirmBooking(booking.id);
      if (!mounted) return;
      Navigator.pushNamedAndRemoveUntil(
        context,
        AppRoutes.bookingDetail,
        ModalRoute.withName(AppRoutes.customerHome),
        arguments: booking.id,
      );
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    final quote = widget.quote;
    return FixFlowScaffold(
      title: 'Confirm booking',
      body: Padding(
        padding: const EdgeInsets.all(16),
        child: Column(
          crossAxisAlignment: CrossAxisAlignment.start,
          children: [
            const Text('Review this booking before confirmation. This records your approval so validation can continue.'),
            const SizedBox(height: 16),
            Text('Technician: ${shortId(quote.technicianId)}'),
            Text('Service: ${widget.request.categoryName ?? widget.request.description}'),
            Text('Total estimate: ${formatMoney(quote.totalAmount, quote.currency)}'),
            Text('Arrival: ${formatDate(quote.arrivalStart)}'),
            Text('Quote expiry: ${formatDate(quote.expiresAt)}'),
            if (_error != null) ...[
              const SizedBox(height: 12),
              Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
            ],
            const Spacer(),
            FilledButton(
              onPressed: _busy ? null : _confirm,
              child: Text(_busy ? 'Confirming…' : 'Confirm Booking'),
            ),
          ],
        ),
      ),
    );
  }
}
