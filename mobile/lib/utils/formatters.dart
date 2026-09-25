import 'package:intl/intl.dart';
import 'package:fixflow_mobile/utils/constants.dart';

String? mediaUrl(String? path) {
  if (path == null || path.isEmpty) return null;
  if (path.startsWith('http://') || path.startsWith('https://')) return path;
  return '${AppConstants.apiBaseUrl}$path';
}

String formatDate(DateTime? value) {
  if (value == null) return '—';
  return DateFormat('d MMM yyyy, HH:mm').format(value.toLocal());
}

String formatMoney(num amount, [String currency = 'LKR']) {
  return NumberFormat.currency(name: currency, decimalDigits: 2).format(amount);
}

String formatStatus(String? status) {
  if (status == null || status.isEmpty) return 'Unknown';
  return status.replaceAll('_', ' ');
}

const bookingStatusLabels = {
  'PENDING_VALIDATION': 'Waiting for your confirmation',
  'CONFIRMED': 'Booking confirmed',
  'ACCEPTED': 'Technician accepted',
  'EN_ROUTE': 'On the way',
  'IN_PROGRESS': 'Work in progress',
  'WORK_COMPLETED': 'Work finished',
  'CUSTOMER_CONFIRMED': 'You confirmed the work',
  'CLOSED': 'Job closed',
  'DISPUTED': 'Dispute opened',
  'CANCELLED': 'Booking cancelled',
};

const bookingStatusDetails = {
  'PENDING_VALIDATION': 'You picked this quote. Confirm it to book the technician.',
  'CONFIRMED': 'Your booking is confirmed. The technician can now see your address.',
  'ACCEPTED': 'The technician accepted the job and is getting ready.',
  'EN_ROUTE': 'The technician is travelling to your location.',
  'IN_PROGRESS': 'The technician is working at your place.',
  'WORK_COMPLETED': 'The technician marked the job as finished. Confirm if you are happy with the work.',
  'CUSTOMER_CONFIRMED': 'You confirmed the work was completed.',
  'CLOSED': 'This job is closed.',
  'DISPUTED': 'A dispute is open on this booking.',
  'CANCELLED': 'This booking was cancelled.',
};

String formatBookingStatus(String? status) => bookingStatusLabels[status] ?? formatStatus(status);

String bookingStatusDetail(String? status) =>
    bookingStatusDetails[status] ?? 'Follow the steps below to see where this job is.';

String shortId(String? id) {
  if (id == null || id.isEmpty) return '—';
  return id.substring(0, id.length < 8 ? id.length : 8);
}
