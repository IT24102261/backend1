import 'package:flutter/material.dart';
import 'package:fixflow_mobile/core/app_theme.dart';
import 'package:fixflow_mobile/utils/formatters.dart';

class SectionCard extends StatelessWidget {
  const SectionCard({super.key, required this.title, required this.child, this.onTap, this.description, this.action});

  final String title;
  final String? description;
  final Widget child;
  final VoidCallback? onTap;
  final Widget? action;

  @override
  Widget build(BuildContext context) {
    final card = Container(
      width: double.infinity,
      margin: const EdgeInsets.only(bottom: 14),
      padding: const EdgeInsets.all(20),
      decoration: BoxDecoration(
        color: Colors.white,
        borderRadius: BorderRadius.circular(24),
        border: Border.all(color: AppColors.cardBorder),
      ),
      child: Column(
        crossAxisAlignment: CrossAxisAlignment.start,
        children: [
          Row(
            crossAxisAlignment: CrossAxisAlignment.end,
            children: [
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(title, style: Theme.of(context).textTheme.titleLarge),
                    if (description != null) ...[
                      const SizedBox(height: 4),
                      Text(description!, style: const TextStyle(color: AppColors.muted, fontSize: 13, height: 1.45)),
                    ],
                  ],
                ),
              ),
              if (action != null) action!,
            ],
          ),
          const SizedBox(height: 16),
          child,
        ],
      ),
    );
    if (onTap == null) return card;
    return GestureDetector(onTap: onTap, child: card);
  }
}

class TimelineView extends StatelessWidget {
  const TimelineView({super.key, required this.steps, required this.current});

  final List<String> steps;
  final String current;

  @override
  Widget build(BuildContext context) {
    var index = steps.indexOf(current);
    if (current == 'CLOSED') index = steps.length - 1;
    return Column(
      children: [
        for (var i = 0; i < steps.length; i++)
          Row(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Column(
                children: [
                  Container(
                    width: 28,
                    height: 28,
                    decoration: BoxDecoration(
                      color: i <= index ? AppColors.ink : Colors.white,
                      shape: BoxShape.circle,
                      border: Border.all(color: i <= index ? AppColors.ink : const Color(0x1A171717)),
                    ),
                    child: Icon(
                      i < index ? Icons.check : Icons.circle,
                      size: i < index ? 14 : 8,
                      color: i <= index ? Colors.white : AppColors.faint,
                    ),
                  ),
                  if (i < steps.length - 1)
                    Container(width: 1, height: 36, color: i < index ? AppColors.gold : const Color(0x1A171717)),
                ],
              ),
              const SizedBox(width: 12),
              Expanded(
                child: Padding(
                  padding: EdgeInsets.only(bottom: i == steps.length - 1 ? 0 : 16, top: 4),
                  child: Column(
                    crossAxisAlignment: CrossAxisAlignment.start,
                    children: [
                      Text(formatBookingStatus(steps[i]), style: const TextStyle(fontWeight: FontWeight.w600, color: AppColors.ink)),
                      const SizedBox(height: 4),
                      Text(bookingStatusDetail(steps[i]), style: const TextStyle(fontSize: 13, color: AppColors.muted, height: 1.4)),
                    ],
                  ),
                ),
              ),
            ],
          ),
      ],
    );
  }
}
