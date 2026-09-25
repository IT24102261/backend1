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

class QuoteCompareScreen extends ConsumerStatefulWidget {
  const QuoteCompareScreen({super.key, required this.request});

  final ServiceRequest request;

  @override
  ConsumerState<QuoteCompareScreen> createState() => _QuoteCompareScreenState();
}

class _QuoteCompareScreenState extends ConsumerState<QuoteCompareScreen> {
  List<Quote> _quotes = [];
  final Map<String, List<Review>> _reviews = {};
  String? _error;
  bool _loading = true;
  String? _aiNote;

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
      final api = ref.read(apiProvider);
      final quotes = await api.quotes(widget.request.id);
      final reviews = <String, List<Review>>{};
      for (final quote in quotes) {
        reviews[quote.technicianId] = await api.technicianReviews(quote.technicianId);
      }
      final history = await api.requestHistory(widget.request.id);
      setState(() {
        _quotes = quotes.where((item) => item.status == 'SENT' || item.status == 'ACCEPTED').toList();
        _reviews.addAll(reviews);
        _aiNote = history
            .map((item) => item.note)
            .whereType<String>()
            .where((note) => note.toLowerCase().contains('match') || note.toLowerCase().contains('invited'))
            .lastOrNull;
      });
    } catch (error) {
      setState(() => _error = error.toString());
    } finally {
      if (mounted) setState(() => _loading = false);
    }
  }

  @override
  Widget build(BuildContext context) {
    return FixFlowScaffold(
      title: 'Compare quotations',
      description: 'We explain the quotations. You choose. FixFlow never books automatically.',
      body: _loading
          ? const Center(child: CircularProgressIndicator())
          : _error != null
              ? ErrorView(message: _error!, onRetry: _load)
              : ListView(
                  padding: const EdgeInsets.all(16),
                  children: [
                    if (_aiNote != null)
                      SectionCard(
                        title: 'What FixFlow noticed',
                        child: Text(
                          '$_aiNote\n\nThis is an explanation only. You pick the quote yourself.',
                        ),
                      ),
                    if (_quotes.isEmpty) const EmptyView(message: 'No eligible quotations yet.'),
                    ..._quotes.map((quote) {
                      final reviews = _reviews[quote.technicianId] ?? [];
                      final published = reviews.where((item) => item.status == 'PUBLISHED').toList();
                      final average = published.isEmpty
                          ? 0.0
                          : published.map((item) => item.rating).reduce((a, b) => a + b) / published.length;
                      final selectedQuote = _quotes.where((item) => item.status == 'ACCEPTED').firstOrNull;
                      final selectionLocked = selectedQuote != null ||
                          const {
                            'BOOKED',
                            'COMPLETED',
                            'CANCELLED',
                          }.contains(widget.request.status);
                      final isSelected = quote.status == 'ACCEPTED' || selectedQuote?.id == quote.id;
                      return Card(
                        margin: const EdgeInsets.only(bottom: 12),
                        child: Padding(
                          padding: const EdgeInsets.all(16),
                          child: Column(
                            crossAxisAlignment: CrossAxisAlignment.start,
                            children: [
                              Row(
                                children: [
                                  CircleAvatar(
                                    radius: 20,
                                    backgroundImage: mediaUrl(quote.profilePhotoUrl) == null
                                        ? null
                                        : NetworkImage(mediaUrl(quote.profilePhotoUrl)!),
                                    child: mediaUrl(quote.profilePhotoUrl) == null ? const Text('T') : null,
                                  ),
                                  const SizedBox(width: 12),
                                  Expanded(child: Text(quote.technicianDisplayName ?? 'Technician ${shortId(quote.technicianId)}', style: Theme.of(context).textTheme.titleMedium)),
                                  const StatusChip(label: 'VERIFIED'),
                                ],
                              ),
                              Text('Rating ${average.toStringAsFixed(1)} · ${published.length} verified reviews'),
                              Text('Completed jobs: ${published.length} reviewed jobs'),
                              const Divider(),
                              Text('Labour ${formatMoney(quote.labourAmount, quote.currency)}'),
                              Text('Materials ${formatMoney(quote.materialsAmount, quote.currency)}'),
                              Text('Travel ${formatMoney(quote.travelAmount, quote.currency)}'),
                              Text('Total ${formatMoney(quote.totalAmount, quote.currency)}', style: const TextStyle(fontWeight: FontWeight.w700)),
                              Text(
                                quote.distanceUnavailable || quote.approximateDistanceKm == null
                                    ? 'Distance unavailable — compare using service area.'
                                    : 'Approx. distance ${quote.approximateDistanceKm} km (${quote.distanceBand ?? 'band n/a'})',
                              ),
                              Text('Arrival ${formatDate(quote.arrivalStart)}'),
                              Text('Duration ${quote.durationMinutes} minutes'),
                              Text('Expires ${formatDate(quote.expiresAt)}'),
                              if (quote.assumptions != null) Text('Assumptions: ${quote.assumptions}'),
                              const SizedBox(height: 12),
                              if (isSelected)
                                const StatusChip(label: 'SELECTED')
                              else if (selectionLocked)
                                Text(
                                  'Not selected — another quotation is already chosen.',
                                  style: Theme.of(context).textTheme.bodySmall,
                                )
                              else if (quote.status == 'SENT')
                                FilledButton(
                                  onPressed: () => Navigator.pushNamed(
                                    context,
                                    AppRoutes.confirmBooking,
                                    arguments: {'request': widget.request, 'quote': quote},
                                  ),
                                  child: const Text('Select Quote'),
                                ),
                            ],
                          ),
                        ),
                      );
                    }),
                  ],
                ),
    );
  }
}
