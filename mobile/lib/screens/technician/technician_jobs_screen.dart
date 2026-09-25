import 'package:flutter/material.dart';
import 'package:flutter_riverpod/flutter_riverpod.dart';
import 'package:url_launcher/url_launcher.dart';
import 'package:fixflow_mobile/models/models.dart';
import 'package:fixflow_mobile/providers/app_providers.dart';
import 'package:fixflow_mobile/routes/app_router.dart';
import 'package:fixflow_mobile/utils/formatters.dart';
import 'package:fixflow_mobile/widgets/async_body.dart';
import 'package:fixflow_mobile/widgets/fixflow_ui.dart';
import 'package:fixflow_mobile/widgets/status_chip.dart';

Uri? googleMapsUri(Booking booking) {
  if (booking.latitude != null && booking.longitude != null) {
    return Uri.parse('https://www.google.com/maps/search/?api=1&query=${booking.latitude},${booking.longitude}');
  }
  if (booking.address != null && booking.address!.trim().isNotEmpty) {
    return Uri.parse('https://www.google.com/maps/search/?api=1&query=${Uri.encodeComponent(booking.address!)}');
  }
  return null;
}

Future<void> openCustomerLocation(Booking booking) async {
  final uri = googleMapsUri(booking);
  if (uri == null) return;
  await launchUrl(uri, mode: LaunchMode.externalApplication);
}

class TechnicianJobsScreen extends ConsumerWidget {
  const TechnicianJobsScreen({super.key});

  @override
  Widget build(BuildContext context, WidgetRef ref) {
    return FixFlowScaffold(
      kind: WorkspaceKind.technician,
      tabIndex: 2,
      title: 'Jobs',
      description: 'Update status and keep booked work moving.',
      body: FutureBuilder(
        future: ref.read(apiProvider).bookings(),
        builder: (context, snapshot) {
          if (snapshot.hasError) return ErrorView(message: snapshot.error.toString());
          if (!snapshot.hasData) return const Center(child: CircularProgressIndicator());
          final items = snapshot.data!.items;
          if (items.isEmpty) return const EmptyView(message: 'No active jobs.');
          return ListView(
            padding: const EdgeInsets.all(16),
            children: items
                .map(
                  (item) => Card(
                    child: ListTile(
                      title: Text(item.customerDisplayName?.isNotEmpty == true ? item.customerDisplayName! : 'Job ${shortId(item.id)}'),
                      subtitle: Text([
                        if (item.categoryName?.isNotEmpty == true) item.categoryName!,
                        if (item.requestDescription?.isNotEmpty == true) item.requestDescription!,
                        item.hasExactLocation ? (item.address ?? 'GPS location ready') : 'Address released after confirmation',
                      ].join('\n')),
                      isThreeLine: true,
                      trailing: StatusChip(label: item.status),
                      onTap: () => Navigator.pushNamed(context, AppRoutes.technicianJobDetail, arguments: item.id),
                    ),
                  ),
                )
                .toList(),
          );
        },
      ),
    );
  }
}

const technicianFlow = ['CONFIRMED', 'ACCEPTED', 'EN_ROUTE', 'IN_PROGRESS', 'WORK_COMPLETED'];

class TechnicianJobDetailScreen extends ConsumerStatefulWidget {
  const TechnicianJobDetailScreen({super.key, required this.bookingId});

  final String bookingId;

  @override
  ConsumerState<TechnicianJobDetailScreen> createState() => _TechnicianJobDetailScreenState();
}

class _TechnicianJobDetailScreenState extends ConsumerState<TechnicianJobDetailScreen> {
  Booking? _booking;
  String? _error;
  bool _loading = true;

  @override
  void initState() {
    super.initState();
    _load();
  }

  Future<void> _load() async {
    try {
      final booking = await ref.read(apiProvider).getBooking(widget.bookingId);
      setState(() => _booking = booking);
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  String? get _next {
    final current = _booking?.status;
    final index = technicianFlow.indexOf(current ?? '');
    if (index == -1 || index == technicianFlow.length - 1) return null;
    return technicianFlow[index + 1];
  }

  @override
  Widget build(BuildContext context) {
    final booking = _booking;
    return FixFlowScaffold(
      title: 'Job status',
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : booking == null
              ? ErrorView(message: _error ?? 'Job not found', onRetry: _load)
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    StatusChip(label: booking.status),
                    const SizedBox(height: 12),
                    Text(booking.customerDisplayName ?? 'Customer', style: Theme.of(context).textTheme.titleLarge),
                    if (booking.customerPhone?.isNotEmpty == true) Text(booking.customerPhone!),
                    if (booking.categoryName?.isNotEmpty == true)
                      Padding(
                        padding: const EdgeInsets.only(top: 8),
                        child: Text(booking.categoryName!, style: Theme.of(context).textTheme.titleMedium),
                      ),
                    if (booking.serviceArea?.isNotEmpty == true) Text(booking.serviceArea!),
                    if (booking.requestDescription?.isNotEmpty == true)
                      Padding(
                        padding: const EdgeInsets.only(top: 12),
                        child: Text(booking.requestDescription!),
                      ),
                    const SizedBox(height: 16),
                    Text(booking.address ?? 'Exact address is hidden until the customer confirms.'),
                    if (booking.latitude != null && booking.longitude != null)
                      Padding(
                        padding: const EdgeInsets.only(top: 8),
                        child: Text('GPS ${booking.latitude!.toStringAsFixed(6)}, ${booking.longitude!.toStringAsFixed(6)}'),
                      ),
                    if (googleMapsUri(booking) != null) ...[
                      const SizedBox(height: 12),
                      FilledButton.icon(
                        onPressed: () => openCustomerLocation(booking),
                        icon: const Icon(Icons.map),
                        label: const Text('Open exact location in Google Maps'),
                      ),
                      const SizedBox(height: 8),
                      OutlinedButton.icon(
                        onPressed: () async {
                          final destination = booking.latitude != null && booking.longitude != null
                              ? '${booking.latitude},${booking.longitude}'
                              : booking.address!;
                          await launchUrl(
                            Uri.parse('https://www.google.com/maps/dir/?api=1&destination=${Uri.encodeComponent(destination)}'),
                            mode: LaunchMode.externalApplication,
                          );
                        },
                        icon: const Icon(Icons.navigation),
                        label: const Text('Start GPS directions'),
                      ),
                    ],
                    const SizedBox(height: 16),
                    if (_next != null)
                      FilledButton(
                        onPressed: () async {
                          try {
                            final updated = await ref.read(apiProvider).updateBookingStatus(booking.id, _next!);
                            setState(() => _booking = updated);
                          } catch (error) {
                            setState(() => _error = error.toString());
                          }
                        },
                        child: Text('Mark ${formatStatus(_next)}'),
                      ),
                    if (_error != null) Text(_error!, style: TextStyle(color: Theme.of(context).colorScheme.error)),
                  ],
                ),
    );
  }
}
