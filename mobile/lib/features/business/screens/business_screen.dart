import 'package:flutter/material.dart';
import 'package:flutter_screenutil/flutter_screenutil.dart';
import 'package:provider/provider.dart';

import '../../../core/theme/app_colors.dart';
import '../../../core/utils/formatters.dart';
import '../../../shared/widgets/async_view.dart';
import '../../../shared/widgets/cards.dart';
import '../../../shared/widgets/state_views.dart';
import '../../../shared/widgets/status_chip.dart';
import '../../auth/providers/auth_provider.dart';
import '../data/business_repository.dart';
import '../models/business_models.dart';
import 'customer_detail_screen.dart';
import 'customer_form_screen.dart';
import 'expense_form_screen.dart';
import 'invoice_detail_screen.dart';
import 'invoice_form_screen.dart';

class BusinessScreen extends StatefulWidget {
  const BusinessScreen({super.key});

  @override
  State<BusinessScreen> createState() => _BusinessScreenState();
}

class _BusinessScreenState extends State<BusinessScreen> with SingleTickerProviderStateMixin {
  final _repo = BusinessRepository();
  late final TabController _tab = TabController(length: 3, vsync: this);

  Future<List<Invoice>>? _invoices;
  Future<List<Customer>>? _customers;
  Future<List<Expense>>? _expenses;

  @override
  void initState() {
    super.initState();
    _load();
    _tab.addListener(() => setState(() {}));
  }

  void _load() {
    final companyId = context.read<AuthProvider>().companyId;
    _invoices = _repo.getInvoices(companyId);
    _customers = _repo.getCustomers(companyId);
    _expenses = _repo.getExpenses(companyId);
  }

  Future<void> _refresh() async {
    setState(_load);
    await Future.wait([_invoices ?? Future.value(), _customers ?? Future.value(), _expenses ?? Future.value()]);
  }

  @override
  void dispose() {
    _tab.dispose();
    super.dispose();
  }

  Future<void> _addCustomer() async {
    final ok = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => const CustomerFormScreen()),
    );
    if (ok == true) _refresh();
  }

  Future<void> _addInvoice() async {
    final ok = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => const InvoiceFormScreen()),
    );
    if (ok == true) _refresh();
  }

  Future<void> _addExpense() async {
    final ok = await Navigator.of(context).push<bool>(
      MaterialPageRoute(builder: (_) => const ExpenseFormScreen()),
    );
    if (ok == true) _refresh();
  }

  @override
  Widget build(BuildContext context) {
    final onCustomersTab = _tab.index == 1;
    final onExpensesTab = _tab.index == 2;
    final auth = context.watch<AuthProvider>();
    final canCreateCustomer = auth.can('customer.create');
    final canCreateInvoice = auth.can('invoice.create');
    final canCreateExpense = auth.can('expense.create');
    return Scaffold(
      appBar: AppBar(
        title: const Text('Business'),
        bottom: TabBar(
          controller: _tab,
          labelColor: AppColors.primary,
          unselectedLabelColor: AppColors.textMuted,
          indicatorColor: AppColors.primary,
          labelStyle: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700),
          tabs: const [Tab(text: 'Invoices'), Tab(text: 'Customers'), Tab(text: 'Expenses')],
        ),
      ),
      floatingActionButton: onExpensesTab
          ? (canCreateExpense
              ? FloatingActionButton.extended(
                  heroTag: 'fab-business-add-expense',
                  onPressed: _addExpense,
                  icon: const Icon(Icons.add_card_rounded),
                  label: const Text('Expense'),
                )
              : null)
          : onCustomersTab
              ? (canCreateCustomer
                  ? FloatingActionButton.extended(
                      heroTag: 'fab-business-add-customer',
                      onPressed: _addCustomer,
                      icon: const Icon(Icons.person_add_alt_rounded),
                      label: const Text('Customer'),
                    )
                  : null)
              : (canCreateInvoice
                  ? FloatingActionButton.extended(
                      heroTag: 'fab-business-add-invoice',
                      onPressed: _addInvoice,
                      icon: const Icon(Icons.receipt_long_rounded),
                      label: const Text('Invoice'),
                    )
                  : null),
      body: SafeArea(
        top: false,
        child: TabBarView(
          controller: _tab,
          children: [
            RefreshIndicator(
              onRefresh: _refresh,
              child: AsyncView<List<Invoice>>(
                future: _invoices,
                onRetry: _refresh,
                builder: (context, items) => _InvoiceList(items: items),
              ),
            ),
            RefreshIndicator(
              onRefresh: _refresh,
              child: AsyncView<List<Customer>>(
                future: _customers,
                onRetry: _refresh,
                builder: (context, items) => _CustomerList(items: items),
              ),
            ),
            RefreshIndicator(
              onRefresh: _refresh,
              child: AsyncView<List<Expense>>(
                future: _expenses,
                onRetry: _refresh,
                builder: (context, items) => _ExpenseList(items: items),
              ),
            ),
          ],
        ),
      ),
    );
  }
}

class _ExpenseList extends StatelessWidget {
  final List<Expense> items;
  const _ExpenseList({required this.items});

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return ListView(children: [
        SizedBox(height: 80.h),
        StatusView.empty(title: 'No expenses', message: 'Recorded costs will appear here.', icon: Icons.receipt_rounded),
      ]);
    }
    return ListView.separated(
      padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 96.h),
      physics: const AlwaysScrollableScrollPhysics(),
      itemCount: items.length,
      separatorBuilder: (context, index) => SizedBox(height: 10.h),
      itemBuilder: (context, i) {
        final e = items[i];
        return AppCard(
          child: Row(
            children: [
              Container(
                width: 40.r,
                height: 40.r,
                decoration: BoxDecoration(color: AppColors.dangerLight, borderRadius: BorderRadius.circular(10.r)),
                child: Icon(Icons.receipt_rounded, size: 18.r, color: AppColors.danger),
              ),
              SizedBox(width: 12.w),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(e.description, maxLines: 1, overflow: TextOverflow.ellipsis,
                        style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700)),
                    SizedBox(height: 2.h),
                    Text(
                      [e.category, e.dogName, if (e.expenseDate != null) Fmt.date(e.expenseDate)]
                          .where((s) => (s ?? '').isNotEmpty)
                          .join('  •  '),
                      maxLines: 1,
                      overflow: TextOverflow.ellipsis,
                      style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary),
                    ),
                  ],
                ),
              ),
              Text(Fmt.money(e.amount), style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w800, color: AppColors.danger)),
            ],
          ),
        );
      },
    );
  }
}

class _InvoiceList extends StatelessWidget {
  final List<Invoice> items;
  const _InvoiceList({required this.items});

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return ListView(children: [
        SizedBox(height: 80.h),
        StatusView.empty(title: 'No invoices', message: 'Invoices created on the portal appear here.', icon: Icons.receipt_long_rounded),
      ]);
    }
    return ListView.separated(
      padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 24.h),
      physics: const AlwaysScrollableScrollPhysics(),
      itemCount: items.length,
      separatorBuilder: (context, index) => SizedBox(height: 10.h),
      itemBuilder: (context, i) {
        final inv = items[i];
        return AppCard(
          onTap: () => Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => InvoiceDetailScreen(invoice: inv)),
          ),
          child: Column(
            crossAxisAlignment: CrossAxisAlignment.start,
            children: [
              Row(
                children: [
                  Expanded(
                    child: Text(inv.invoiceNumber.isEmpty ? 'Invoice #${inv.id}' : inv.invoiceNumber,
                        maxLines: 1, overflow: TextOverflow.ellipsis,
                        style: TextStyle(fontSize: 15.sp, fontWeight: FontWeight.w700)),
                  ),
                  StatusChip.forStatus(inv.status),
                ],
              ),
              SizedBox(height: 4.h),
              Text(inv.customerName ?? 'No customer',
                  maxLines: 1, overflow: TextOverflow.ellipsis,
                  style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary)),
              SizedBox(height: 12.h),
              Row(
                children: [
                  Expanded(child: _amount('Total', inv.totalAmount, AppColors.textPrimary)),
                  Expanded(child: _amount('Paid', inv.paidAmount, AppColors.success)),
                  Expanded(child: _amount('Balance', inv.balanceAmount, inv.balanceAmount > 0 ? AppColors.danger : AppColors.textMuted)),
                ],
              ),
              SizedBox(height: 8.h),
              Text('Due ${Fmt.date(inv.dueDate)}', style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
            ],
          ),
        );
      },
    );
  }

  Widget _amount(String label, double value, Color color) {
    return Column(
      crossAxisAlignment: CrossAxisAlignment.start,
      children: [
        Text(label, style: TextStyle(fontSize: 11.sp, color: AppColors.textMuted)),
        SizedBox(height: 2.h),
        FittedBox(
          fit: BoxFit.scaleDown,
          alignment: Alignment.centerLeft,
          child: Text(Fmt.money(value), style: TextStyle(fontSize: 13.sp, fontWeight: FontWeight.w700, color: color)),
        ),
      ],
    );
  }
}

class _CustomerList extends StatelessWidget {
  final List<Customer> items;
  const _CustomerList({required this.items});

  @override
  Widget build(BuildContext context) {
    if (items.isEmpty) {
      return ListView(children: [
        SizedBox(height: 80.h),
        StatusView.empty(title: 'No customers', message: 'Add a customer to get started.', icon: Icons.people_alt_rounded),
      ]);
    }
    return ListView.separated(
      padding: EdgeInsets.fromLTRB(16.w, 12.h, 16.w, 96.h),
      physics: const AlwaysScrollableScrollPhysics(),
      itemCount: items.length,
      separatorBuilder: (context, index) => SizedBox(height: 10.h),
      itemBuilder: (context, i) {
        final c = items[i];
        final name = c.displayName.isEmpty ? 'Unnamed' : c.displayName;
        return AppCard(
          onTap: () => Navigator.of(context).push(
            MaterialPageRoute(builder: (_) => CustomerDetailScreen(customer: c)),
          ),
          child: Row(
            children: [
              CircleAvatar(
                radius: 22.r,
                backgroundColor: AppColors.primaryLight,
                child: Text(
                  name.substring(0, 1).toUpperCase(),
                  style: TextStyle(color: AppColors.primary, fontWeight: FontWeight.w800, fontSize: 16.sp),
                ),
              ),
              SizedBox(width: 12.w),
              Expanded(
                child: Column(
                  crossAxisAlignment: CrossAxisAlignment.start,
                  children: [
                    Text(name, maxLines: 1, overflow: TextOverflow.ellipsis,
                        style: TextStyle(fontSize: 14.sp, fontWeight: FontWeight.w700)),
                    SizedBox(height: 2.h),
                    if ((c.cellphone ?? '').isNotEmpty)
                      _meta(Icons.phone_outlined, c.cellphone!),
                    if ((c.email ?? '').isNotEmpty)
                      _meta(Icons.email_outlined, c.email!),
                  ],
                ),
              ),
              Icon(Icons.chevron_right_rounded, color: AppColors.textMuted, size: 22.r),
            ],
          ),
        );
      },
    );
  }

  Widget _meta(IconData icon, String text) => Padding(
        padding: EdgeInsets.only(top: 2.h),
        child: Row(
          children: [
            Icon(icon, size: 13.r, color: AppColors.textMuted),
            SizedBox(width: 6.w),
            Expanded(child: Text(text, maxLines: 1, overflow: TextOverflow.ellipsis,
                style: TextStyle(fontSize: 12.sp, color: AppColors.textSecondary))),
          ],
        ),
      );
}
