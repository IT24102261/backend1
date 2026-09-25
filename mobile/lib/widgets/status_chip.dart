import 'package:flutter/material.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/utils/formatters.dart';

class StatusChip extends StatelessWidget {
  const StatusChip({super.key, required this.label, this.display});

  final String label;
  final String? display;

  @override
  Widget build(BuildContext context) {
    final tone = _tone(label);
    return Container(
      padding: const EdgeInsets.symmetric(horizontal: 12, vertical: 5),
      decoration: BoxDecoration(
        color: tone.$1,
        borderRadius: BorderRadius.circular(999),
        border: Border.all(color: tone.$2),
      ),
      child: Text(
        display ?? formatStatus(label),
        style: TextStyle(color: tone.$3, fontSize: 11, fontWeight: FontWeight.w700),
      ),
    );
  }

  (Color, Color, Color) _tone(String status) {
    const cream = AppColors.cream;
    const goldText = Color(0xFF7A6240);
    const goldRing = AppColors.goldSoft;
    switch (status) {
      case 'APPROVED':
      case 'ACCEPTED':
      case 'COMPLETED':
      case 'PUBLISHED':
      case 'RESOLVED':
      case 'CUSTOMER_CONFIRMED':
      case 'SUCCESS':
        return (const Color(0xFFECFDF5), const Color(0xFFA7F3D0), const Color(0xFF047857));
      case 'REJECTED':
      case 'FAILED':
      case 'CANCELLED':
      case 'DISPUTED':
      case 'DECLINED':
        return (const Color(0xFFFFF1F2), const Color(0xFFFECDD3), const Color(0xFFBE123C));
      case 'PENDING':
      case 'PENDING_VALIDATION':
      case 'OPEN':
      case 'WAITING_APPROVAL':
        return (const Color(0xFFFFFBEB), const Color(0xFFFDE68A), const Color(0xFF92400E));
      case 'SUBMITTED':
      case 'SENT':
      case 'RUNNING':
        return (AppColors.navy, AppColors.navy, AppColors.cream);
      case 'MATCHING':
      case 'COLLECTING_QUOTES':
      case 'IN_PROGRESS':
      case 'WORK_COMPLETED':
      case 'ACTIVE':
        return (cream, goldRing, goldText);
      case 'CONFIRMED':
      case 'BOOKED':
        return (const Color(0xFFEFF6FF), const Color(0xFFBFDBFE), const Color(0xFF1D4ED8));
      default:
        return (const Color(0xFFF1F5F9), const Color(0xFFE2E8F0), const Color(0xFF475569));
    }
  }
}
