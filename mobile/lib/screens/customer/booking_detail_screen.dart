import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/section_card.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

const bookingSteps = [
  'PENDING_VALIDATION',
  'CONFIRMED',
  'ACCEPTED',
  'EN_ROUTE',
  'IN_PROGRESS',
  'WORK_COMPLETED',
  'CUSTOMER_CONFIRMED',
];

class BookingDetailScreen extends ConsumerStatefulWidget {
  const BookingDetailScreen({super.key, required this.bookingId});

  final String bookingId;

  @override
  ConsumerState<BookingDetailScreen> createState() => _BookingDetailScreenState();
}

class _BookingDetailScreenState extends ConsumerState<BookingDetailScreen> {
  Booking? _booking;
  String? _error;
  bool _loading = true;
  bool _busy = false;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    setState(() {
      _loading = true;
      _error = null;
    });
    try {
      final booking = await ref.read(apiProvider).getBooking(widget.bookingId);
      setState(() => _booking = booking);
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  Future<void> _confirmCompletion() async {
    setState(() => _busy = true);
    try {
      final updated = await ref.read(apiProvider).updateBookingStatus(widget.bookingId, 'CUSTOMER_CONFIRMED');
      setState(() => _booking = updated);
      if (!mounted) return;
      await Navigator.pushNamed(context, AppRoutes.review, arguments: updated);
      await _load();
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _busy = false);
    }
  }

  Future<void> _dispute() async {
    final controller = TextEditingController();
    final confirmed = await showDialog<bool>(
      context: context,
      builder: (context) => AlertDialog(
        title: const Text('Open dispute'),
        content: TextField(controller: controller, decoration: const InputDecoration(labelText: 'What went wrong?'), maxLines: 3),
        actions: [
          TextButton(onPressed: () => Navigator.pop(context, false), child: const Text('Cancel')),
          FilledButton(onPressed: () => Navigator.pop(context, true), child: const Text('Submit')),
        ],
      ),
    );
    if (confirmed != true || controller.text.trim().isEmpty) return;
    try {
      await ref.read(apiProvider).createComplaint(
            bookingId: widget.bookingId,
            subject: 'Booking dispute',
            description: controller.text.trim(),
          );
      await ref.read(apiProvider).updateBookingStatus(widget.bookingId, 'DISPUTED');
      await _load();
    } catch (error) {
      setState(() => _error = error.toString());
    }
  }

  @override
  Widget build(BuildContext context) {
    final booking = _booking;
    return FixFlowScaffold(
      title: 'Track job',
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null && booking == null
              ? ErrorView(message: _error!, onRetry: _load)
              : booking == null
                  ? const EmptyView(message: 'Booking not found.')
                  : ListView(
                      padding: const EdgeInsets.all(16),
                      children: [
                        if (_error != null) Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                        StatusChip(label: formatBookingStatus(booking.status)),
                        Padding(
                          padding: const EdgeInsets.only(top: 12),
                          child: Row(
                            children: [
                              CircleAvatar(
                                radius: 28,
                                backgroundImage: mediaUrl(booking.profilePhotoUrl) == null
                                    ? null
                                    : NetworkImage(mediaUrl(booking.profilePhotoUrl)!),
                                child: mediaUrl(booking.profilePhotoUrl) == null
                                    ? Text(((booking.technicianDisplayName ?? 'T').trim().isEmpty
                                            ? 'T'
                                            : booking.technicianDisplayName!.trim()[0])
                                        .toUpperCase())
                                    : null,
                              ),
                              const SizedBox(width: 12),
                              Expanded(
                                child: Text(
                                  booking.technicianDisplayName ?? 'Technician',
                                  style: Theme.of(context).textTheme.titleMedium,
                                ),
                              ),
                            ],
                          ),
                        ),
                        Padding(
                          padding: const EdgeInsets.only(top: 8),
                          child: Text(bookingStatusDetail(booking.status)),
                        ),
                        if (booking.address != null) Padding(padding: const EdgeInsets.only(top: 8), child: Text('Address: ${booking.address}')),
                        SectionCard(
                          title: 'Track your job',
                          child: TimelineView(steps: bookingSteps, current: booking.status),
                        ),
                        if (booking.status == 'WORK_COMPLETED') ...[
                          FilledButton(
                            onPressed: _busy ? null : _confirmCompletion,
                            child: const Text('Confirm Completion'),
                          ),
                          const SizedBox(height: 8),
                          OutlinedButton(onPressed: _dispute, child: const Text('Open Dispute')),
                        ],
                        if (booking.status == 'CUSTOMER_CONFIRMED' || booking.status == 'CLOSED')
                          FilledButton(
                            onPressed: () => Navigator.pushNamed(context, AppRoutes.review, arguments: booking),
                            child: const Text('Leave a review'),
                          ),
                      ],
                    ),
    );
  }
}
