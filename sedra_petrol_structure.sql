-- Sedra Petrol database structure (SQL Server)
-- Generated from SedraPetrol.Api/Migrations/AppDbContextModelSnapshot.cs, 92 tables.
-- Tables are grouped by module. Order is for reading; to create them use the EF migrations.

-- =====================================================
-- Accounting & general ledger
-- =====================================================
-- account  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[account] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [account_code]       nvarchar(20) NOT NULL,
    [account_type_id]    bigint NULL,
    [company_id]         bigint NOT NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [currency_id]        bigint NULL,
    [is_active]          bit NOT NULL,
    [is_deleted]         bit NOT NULL,
    [is_group]           bit NOT NULL,
    [is_postable]        bit NOT NULL,
    [level]              smallint NULL,
    [name_ar]            nvarchar(200) NULL,
    [name_en]            nvarchar(200) NULL,
    [parent_account_id]  bigint NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    CONSTRAINT [PK_account] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_account_company_id_account_code] UNIQUE ([company_id], [account_code]),
    CONSTRAINT [FK_account_account_type_id] FOREIGN KEY ([account_type_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_account_company_id] FOREIGN KEY ([company_id]) REFERENCES [company]([id]),
    CONSTRAINT [FK_account_currency_id] FOREIGN KEY ([currency_id]) REFERENCES [currency]([id]),
    CONSTRAINT [FK_account_parent_account_id] FOREIGN KEY ([parent_account_id]) REFERENCES [account]([id])
);
GO

-- account_mapping  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[account_mapping] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [branch_id]          bigint NULL,
    [company_id]         bigint NOT NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [credit_account_id]  bigint NULL,
    [debit_account_id]   bigint NULL,
    [is_active]          bit NOT NULL,
    [is_deleted]         bit NOT NULL,
    [source_key_id]      bigint NULL,
    [source_type]        nvarchar(50) NOT NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    CONSTRAINT [PK_account_mapping] PRIMARY KEY ([id]),
    CONSTRAINT [FK_account_mapping_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_account_mapping_company_id] FOREIGN KEY ([company_id]) REFERENCES [company]([id]),
    CONSTRAINT [FK_account_mapping_credit_account_id] FOREIGN KEY ([credit_account_id]) REFERENCES [account]([id]),
    CONSTRAINT [FK_account_mapping_debit_account_id] FOREIGN KEY ([debit_account_id]) REFERENCES [account]([id]),
    CONSTRAINT [FK_account_mapping_source_key_id] FOREIGN KEY ([source_key_id]) REFERENCES [lookup_value]([id])
);
GO

-- accounting_setting  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[accounting_setting] (
    [id]                          bigint IDENTITY(1,1) NOT NULL,
    [company_id]                  bigint NOT NULL,
    [created_at]                  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]                  bigint NULL,
    [is_deleted]                  bit NOT NULL,
    [journal_approval_threshold]  decimal(14,2) NOT NULL,
    [opening_equity_account_id]   bigint NULL,
    [updated_at]                  datetimeoffset NULL,
    [updated_by]                  bigint NULL,
    CONSTRAINT [PK_accounting_setting] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_accounting_setting_company_id_ux_accounting_setting_company] UNIQUE ([company_id], [ux_accounting_setting_company]),
    CONSTRAINT [FK_accounting_setting_company_id] FOREIGN KEY ([company_id]) REFERENCES [company]([id]),
    CONSTRAINT [FK_accounting_setting_opening_equity_account_id] FOREIGN KEY ([opening_equity_account_id]) REFERENCES [account]([id])
);
GO

-- journal_entry  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[journal_entry] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [branch_id]          bigint NOT NULL,
    [company_id]         bigint NOT NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [currency_id]        bigint NULL,
    [description]        nvarchar(max) NULL,
    [entry_date]         date NOT NULL,
    [exchange_rate]      decimal(18,6) NULL,
    [fiscal_period_id]   bigint NOT NULL,
    [is_deleted]         bit NOT NULL,
    [journal_no]         nvarchar(30) NOT NULL,
    [posted_at]          datetimeoffset NULL,
    [posted_by]          bigint NULL,
    [reversed_entry_id]  bigint NULL,
    [source_id]          bigint NULL,
    [source_type]        nvarchar(50) NULL,
    [status_id]          bigint NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    CONSTRAINT [PK_journal_entry] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_journal_entry_journal_no] UNIQUE ([journal_no]),
    CONSTRAINT [FK_journal_entry_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_journal_entry_company_id] FOREIGN KEY ([company_id]) REFERENCES [company]([id]),
    CONSTRAINT [FK_journal_entry_currency_id] FOREIGN KEY ([currency_id]) REFERENCES [currency]([id]),
    CONSTRAINT [FK_journal_entry_fiscal_period_id] FOREIGN KEY ([fiscal_period_id]) REFERENCES [fiscal_period]([id]),
    CONSTRAINT [FK_journal_entry_posted_by] FOREIGN KEY ([posted_by]) REFERENCES [app_user]([id]),
    CONSTRAINT [FK_journal_entry_reversed_entry_id] FOREIGN KEY ([reversed_entry_id]) REFERENCES [journal_entry]([id]),
    CONSTRAINT [FK_journal_entry_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id])
);
GO

-- journal_line  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[journal_line] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [account_id]        bigint NOT NULL,
    [base_credit]       decimal(14,2) NULL,
    [base_debit]        decimal(14,2) NULL,
    [building_id]       bigint NULL,
    [contract_id]       bigint NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [credit]            decimal(14,2) NULL,
    [debit]             decimal(14,2) NULL,
    [description]       nvarchar(300) NULL,
    [is_deleted]        bit NOT NULL,
    [journal_entry_id]  bigint NOT NULL,
    [tenant_id]         bigint NULL,
    [unit_id]           bigint NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    [vendor_id]         bigint NULL,
    CONSTRAINT [PK_journal_line] PRIMARY KEY ([id]),
    CONSTRAINT [FK_journal_line_account_id] FOREIGN KEY ([account_id]) REFERENCES [account]([id]),
    CONSTRAINT [FK_journal_line_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_journal_line_contract_id] FOREIGN KEY ([contract_id]) REFERENCES [supply_contract]([id]),
    CONSTRAINT [FK_journal_line_journal_entry_id] FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry]([id]),
    CONSTRAINT [FK_journal_line_tenant_id] FOREIGN KEY ([tenant_id]) REFERENCES [tenant]([id]),
    CONSTRAINT [FK_journal_line_unit_id] FOREIGN KEY ([unit_id]) REFERENCES [unit]([id]),
    CONSTRAINT [FK_journal_line_vendor_id] FOREIGN KEY ([vendor_id]) REFERENCES [vendor]([id])
);
GO

-- journal_approval  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[journal_approval] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [amount]            decimal(14,2) NOT NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [decided_at]        datetimeoffset NULL,
    [decided_by]        bigint NULL,
    [is_deleted]        bit NOT NULL,
    [journal_entry_id]  bigint NOT NULL,
    [reason]            nvarchar(500) NULL,
    [requested_at]      datetimeoffset NOT NULL,
    [requested_by]      bigint NULL,
    [status]            nvarchar(20) NOT NULL,
    [threshold]         decimal(14,2) NOT NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    CONSTRAINT [PK_journal_approval] PRIMARY KEY ([id]),
    CONSTRAINT [FK_journal_approval_journal_entry_id] FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry]([id])
);
GO

-- fiscal_period  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[fiscal_period] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [company_id]  bigint NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [end_date]    date NOT NULL,
    [is_closed]   bit NOT NULL,
    [is_deleted]  bit NOT NULL,
    [period_no]   smallint NOT NULL,
    [start_date]  date NOT NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    [year]        smallint NOT NULL,
    CONSTRAINT [PK_fiscal_period] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_fiscal_period_company_id_year_period_no] UNIQUE ([company_id], [year], [period_no]),
    CONSTRAINT [FK_fiscal_period_company_id] FOREIGN KEY ([company_id]) REFERENCES [company]([id])
);
GO

-- tax_rate  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[tax_rate] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [applies_to]         nvarchar(10) NOT NULL,
    [category]           nvarchar(20) NOT NULL,
    [code]               nvarchar(30) NOT NULL,
    [company_id]         bigint NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [input_account_id]   bigint NULL,
    [is_active]          bit NOT NULL,
    [is_default]         bit NOT NULL,
    [is_deleted]         bit NOT NULL,
    [name_ar]            nvarchar(150) NULL,
    [name_en]            nvarchar(150) NULL,
    [notes]              nvarchar(500) NULL,
    [output_account_id]  bigint NULL,
    [rate]               decimal(5,2) NOT NULL,
    [tax_type]           nvarchar(20) NOT NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    [valid_from]         date NOT NULL,
    [valid_to]           date NULL,
    CONSTRAINT [PK_tax_rate] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_tax_rate_code_valid_from_ux_tax_rate_code_from] UNIQUE ([code], [valid_from], [ux_tax_rate_code_from]),
    CONSTRAINT [FK_tax_rate_input_account_id] FOREIGN KEY ([input_account_id]) REFERENCES [account]([id]),
    CONSTRAINT [FK_tax_rate_output_account_id] FOREIGN KEY ([output_account_id]) REFERENCES [account]([id])
);
GO

-- cashflow_item  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[cashflow_item] (
    [id]            bigint IDENTITY(1,1) NOT NULL,
    [amount]        decimal(14,2) NOT NULL,
    [branch_id]     bigint NULL,
    [building_id]   bigint NULL,
    [category]      nvarchar(50) NOT NULL,
    [confidence]    nvarchar(10) NOT NULL,
    [counterparty]  nvarchar(200) NULL,
    [created_at]    datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]    bigint NULL,
    [description]   nvarchar(500) NULL,
    [direction]     nvarchar(3) NOT NULL,
    [end_date]      date NULL,
    [is_active]     bit NOT NULL,
    [is_deleted]    bit NOT NULL,
    [recurrence]    nvarchar(10) NOT NULL,
    [start_date]    date NOT NULL,
    [updated_at]    datetimeoffset NULL,
    [updated_by]    bigint NULL,
    CONSTRAINT [PK_cashflow_item] PRIMARY KEY ([id]),
    CONSTRAINT [FK_cashflow_item_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_cashflow_item_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id])
);
GO

-- currency  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[currency] (
    [id]              bigint IDENTITY(1,1) NOT NULL,
    [code]            nvarchar(3) NOT NULL,
    [created_at]      datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]      bigint NULL,
    [decimal_places]  smallint NULL,
    [is_active]       bit NOT NULL,
    [is_deleted]      bit NOT NULL,
    [name_ar]         nvarchar(50) NULL,
    [name_en]         nvarchar(50) NULL,
    [symbol]          nvarchar(10) NULL,
    [updated_at]      datetimeoffset NULL,
    [updated_by]      bigint NULL,
    CONSTRAINT [PK_currency] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_currency_code] UNIQUE ([code])
);
GO


-- =====================================================
-- Banking
-- =====================================================
-- bank_account  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[bank_account] (
    [id]                  bigint IDENTITY(1,1) NOT NULL,
    [account_no]          nvarchar(50) NULL,
    [account_type]        nvarchar(20) NOT NULL,
    [bank_name]           nvarchar(150) NULL,
    [branch_id]           bigint NULL,
    [created_at]          datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]          bigint NULL,
    [currency_id]         bigint NULL,
    [gl_account_id]       bigint NOT NULL,
    [iban]                nvarchar(50) NULL,
    [is_active]           bit NOT NULL,
    [is_deleted]          bit NOT NULL,
    [name_ar]             nvarchar(150) NOT NULL,
    [name_en]             nvarchar(150) NULL,
    [notes]               nvarchar(500) NULL,
    [opening_balance]     decimal(14,2) NOT NULL,
    [opening_date]        date NULL,
    [opening_journal_id]  bigint NULL,
    [updated_at]          datetimeoffset NULL,
    [updated_by]          bigint NULL,
    CONSTRAINT [PK_bank_account] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_bank_account_gl_account_id_ux_bank_account_gl] UNIQUE ([gl_account_id], [ux_bank_account_gl]),
    CONSTRAINT [FK_bank_account_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_bank_account_currency_id] FOREIGN KEY ([currency_id]) REFERENCES [currency]([id]),
    CONSTRAINT [FK_bank_account_gl_account_id] FOREIGN KEY ([gl_account_id]) REFERENCES [account]([id]),
    CONSTRAINT [FK_bank_account_opening_journal_id] FOREIGN KEY ([opening_journal_id]) REFERENCES [journal_entry]([id])
);
GO

-- bank_transfer  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[bank_transfer] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [amount]            decimal(14,2) NOT NULL,
    [branch_id]         bigint NOT NULL,
    [cancel_reason]     nvarchar(300) NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [fees]              decimal(14,2) NOT NULL,
    [fees_account_id]   bigint NULL,
    [from_account_id]   bigint NOT NULL,
    [is_deleted]        bit NOT NULL,
    [journal_entry_id]  bigint NULL,
    [notes]             nvarchar(500) NULL,
    [reference_no]      nvarchar(100) NULL,
    [status]            nvarchar(20) NOT NULL,
    [to_account_id]     bigint NOT NULL,
    [transfer_date]     date NOT NULL,
    [transfer_no]       nvarchar(30) NOT NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    CONSTRAINT [PK_bank_transfer] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_bank_transfer_transfer_no_ux_bank_transfer_no] UNIQUE ([transfer_no], [ux_bank_transfer_no]),
    CONSTRAINT [FK_bank_transfer_fees_account_id] FOREIGN KEY ([fees_account_id]) REFERENCES [account]([id]),
    CONSTRAINT [FK_bank_transfer_from_account_id] FOREIGN KEY ([from_account_id]) REFERENCES [bank_account]([id]),
    CONSTRAINT [FK_bank_transfer_journal_entry_id] FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry]([id]),
    CONSTRAINT [FK_bank_transfer_to_account_id] FOREIGN KEY ([to_account_id]) REFERENCES [bank_account]([id])
);
GO

-- bank_statement_line  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[bank_statement_line] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [amount]             decimal(14,2) NOT NULL,
    [balance]            decimal(14,2) NULL,
    [bank_account_id]    bigint NOT NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [description]        nvarchar(500) NULL,
    [import_batch]       uniqueidentifier NOT NULL,
    [is_deleted]         bit NOT NULL,
    [journal_line_id]    bigint NULL,
    [line_date]          date NOT NULL,
    [matched_at]         datetimeoffset NULL,
    [matched_by]         bigint NULL,
    [reconciliation_id]  bigint NULL,
    [reference]          nvarchar(100) NULL,
    [status]             nvarchar(20) NOT NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    CONSTRAINT [PK_bank_statement_line] PRIMARY KEY ([id]),
    CONSTRAINT [FK_bank_statement_line_bank_account_id] FOREIGN KEY ([bank_account_id]) REFERENCES [bank_account]([id]),
    CONSTRAINT [FK_bank_statement_line_journal_line_id] FOREIGN KEY ([journal_line_id]) REFERENCES [journal_line]([id]),
    CONSTRAINT [FK_bank_statement_line_reconciliation_id] FOREIGN KEY ([reconciliation_id]) REFERENCES [bank_reconciliation]([id])
);
GO

-- bank_reconciliation  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[bank_reconciliation] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [bank_account_id]    bigint NOT NULL,
    [book_balance]       decimal(14,2) NOT NULL,
    [completed_at]       datetimeoffset NOT NULL,
    [completed_by]       bigint NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [difference]         decimal(14,2) NOT NULL,
    [is_deleted]         bit NOT NULL,
    [notes]              nvarchar(500) NULL,
    [outstanding_book]   decimal(14,2) NOT NULL,
    [period_from]        date NOT NULL,
    [period_to]          date NOT NULL,
    [statement_balance]  decimal(14,2) NOT NULL,
    [status]             nvarchar(20) NOT NULL,
    [unrecorded_bank]    decimal(14,2) NOT NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    CONSTRAINT [PK_bank_reconciliation] PRIMARY KEY ([id]),
    CONSTRAINT [FK_bank_reconciliation_bank_account_id] FOREIGN KEY ([bank_account_id]) REFERENCES [bank_account]([id])
);
GO


-- =====================================================
-- Sales, receipts & customer credit
-- =====================================================
-- invoice  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[invoice] (
    [id]                    bigint IDENTITY(1,1) NOT NULL,
    [billing_period_end]    date NULL,
    [billing_period_start]  date NULL,
    [branch_id]             bigint NOT NULL,
    [building_id]           bigint NULL,
    [contract_id]           bigint NULL,
    [created_at]            datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]            bigint NULL,
    [currency_id]           bigint NULL,
    [due_date]              date NOT NULL,
    [exchange_rate]         decimal(18,6) NULL,
    [invoice_no]            nvarchar(30) NOT NULL,
    [invoice_type_id]       bigint NULL,
    [is_deleted]            bit NOT NULL,
    [issue_date]            date NOT NULL,
    [journal_entry_id]      bigint NULL,
    [notes]                 nvarchar(max) NULL,
    [status_id]             bigint NULL,
    [subtotal]              decimal(14,2) NULL,
    [tenant_id]             bigint NOT NULL,
    [total_amount]          decimal(14,2) NULL,
    [updated_at]            datetimeoffset NULL,
    [updated_by]            bigint NULL,
    [vat_amount]            decimal(14,2) NULL,
    CONSTRAINT [PK_invoice] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_invoice_invoice_no] UNIQUE ([invoice_no]),
    CONSTRAINT [FK_invoice_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_invoice_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_invoice_contract_id] FOREIGN KEY ([contract_id]) REFERENCES [supply_contract]([id]),
    CONSTRAINT [FK_invoice_currency_id] FOREIGN KEY ([currency_id]) REFERENCES [currency]([id]),
    CONSTRAINT [FK_invoice_invoice_type_id] FOREIGN KEY ([invoice_type_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_invoice_journal_entry_id] FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry]([id]),
    CONSTRAINT [FK_invoice_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_invoice_tenant_id] FOREIGN KEY ([tenant_id]) REFERENCES [tenant]([id])
);
GO

-- invoice_item  (created in migration InitialCreate, 17 Oct)
-- columns added later: tax_rate_id (allsp 19 Oct)
CREATE TABLE [dbo].[invoice_item] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [amount]            decimal(14,2) NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [description]       nvarchar(300) NULL,
    [invoice_id]        bigint NOT NULL,
    [is_deleted]        bit NOT NULL,
    [item_type_id]      bigint NULL,
    [meter_reading_id]  bigint NULL,
    [quantity]          decimal(14,3) NULL,
    [tax_rate_id]       bigint NULL,
    [unit_price]        decimal(14,4) NULL,
    [uom]               nvarchar(10) NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    [vat_rate]          decimal(5,2) NULL,
    CONSTRAINT [PK_invoice_item] PRIMARY KEY ([id]),
    CONSTRAINT [FK_invoice_item_invoice_id] FOREIGN KEY ([invoice_id]) REFERENCES [invoice]([id]),
    CONSTRAINT [FK_invoice_item_item_type_id] FOREIGN KEY ([item_type_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_invoice_item_meter_reading_id] FOREIGN KEY ([meter_reading_id]) REFERENCES [meter_reading]([id])
);
GO

-- invoice_installment  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[invoice_installment] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [amount]      decimal(14,2) NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [due_date]    date NOT NULL,
    [invoice_id]  bigint NOT NULL,
    [is_deleted]  bit NOT NULL,
    [notes]       nvarchar(500) NULL,
    [seq_no]      smallint NOT NULL,
    [status]      nvarchar(20) NOT NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    CONSTRAINT [PK_invoice_installment] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_invoice_installment_invoice_id_seq_no_ux_invoice_installment_seq] UNIQUE ([invoice_id], [seq_no], [ux_invoice_installment_seq]),
    CONSTRAINT [FK_invoice_installment_invoice_id] FOREIGN KEY ([invoice_id]) REFERENCES [invoice]([id])
);
GO

-- payment  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[payment] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [amount]             decimal(14,2) NOT NULL,
    [branch_id]          bigint NOT NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [currency_id]        bigint NULL,
    [is_deleted]         bit NOT NULL,
    [journal_entry_id]   bigint NULL,
    [notes]              nvarchar(max) NULL,
    [payment_date]       date NOT NULL,
    [payment_method_id]  bigint NULL,
    [receipt_no]         nvarchar(30) NOT NULL,
    [received_by]        bigint NULL,
    [reference_no]       nvarchar(100) NULL,
    [status_id]          bigint NULL,
    [tenant_id]          bigint NOT NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    CONSTRAINT [PK_payment] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_payment_receipt_no] UNIQUE ([receipt_no]),
    CONSTRAINT [FK_payment_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_payment_currency_id] FOREIGN KEY ([currency_id]) REFERENCES [currency]([id]),
    CONSTRAINT [FK_payment_journal_entry_id] FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry]([id]),
    CONSTRAINT [FK_payment_payment_method_id] FOREIGN KEY ([payment_method_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_payment_received_by] FOREIGN KEY ([received_by]) REFERENCES [app_user]([id]),
    CONSTRAINT [FK_payment_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_payment_tenant_id] FOREIGN KEY ([tenant_id]) REFERENCES [tenant]([id])
);
GO

-- payment_allocation  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[payment_allocation] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [amount]      decimal(14,2) NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [invoice_id]  bigint NOT NULL,
    [is_deleted]  bit NOT NULL,
    [payment_id]  bigint NOT NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    CONSTRAINT [PK_payment_allocation] PRIMARY KEY ([id]),
    CONSTRAINT [FK_payment_allocation_invoice_id] FOREIGN KEY ([invoice_id]) REFERENCES [invoice]([id]),
    CONSTRAINT [FK_payment_allocation_payment_id] FOREIGN KEY ([payment_id]) REFERENCES [payment]([id])
);
GO

-- credit_note  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[credit_note] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [branch_id]         bigint NOT NULL,
    [building_id]       bigint NULL,
    [cancel_reason]     nvarchar(500) NULL,
    [contract_id]       bigint NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [credit_note_no]    nvarchar(30) NOT NULL,
    [invoice_id]        bigint NOT NULL,
    [is_deleted]        bit NOT NULL,
    [issue_date]        date NOT NULL,
    [journal_entry_id]  bigint NULL,
    [reason]            nvarchar(500) NOT NULL,
    [status]            nvarchar(20) NOT NULL,
    [subtotal]          decimal(14,2) NOT NULL,
    [tenant_id]         bigint NOT NULL,
    [total_amount]      decimal(14,2) NOT NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    [vat_amount]        decimal(14,2) NOT NULL,
    CONSTRAINT [PK_credit_note] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_credit_note_credit_note_no_ux_credit_note_no] UNIQUE ([credit_note_no], [ux_credit_note_no]),
    CONSTRAINT [FK_credit_note_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_credit_note_invoice_id] FOREIGN KEY ([invoice_id]) REFERENCES [invoice]([id]),
    CONSTRAINT [FK_credit_note_journal_entry_id] FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry]([id]),
    CONSTRAINT [FK_credit_note_tenant_id] FOREIGN KEY ([tenant_id]) REFERENCES [tenant]([id])
);
GO

-- credit_note_line  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[credit_note_line] (
    [id]               bigint IDENTITY(1,1) NOT NULL,
    [amount]           decimal(14,2) NOT NULL,
    [created_at]       datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]       bigint NULL,
    [credit_note_id]   bigint NOT NULL,
    [description]      nvarchar(500) NULL,
    [invoice_item_id]  bigint NULL,
    [is_deleted]       bit NOT NULL,
    [item_type_id]     bigint NULL,
    [updated_at]       datetimeoffset NULL,
    [updated_by]       bigint NULL,
    [vat_amount]       decimal(14,2) NOT NULL,
    [vat_rate]         decimal(5,2) NOT NULL,
    CONSTRAINT [PK_credit_note_line] PRIMARY KEY ([id]),
    CONSTRAINT [FK_credit_note_line_credit_note_id] FOREIGN KEY ([credit_note_id]) REFERENCES [credit_note]([id]),
    CONSTRAINT [FK_credit_note_line_invoice_item_id] FOREIGN KEY ([invoice_item_id]) REFERENCES [invoice_item]([id])
);
GO

-- credit_note_allocation  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[credit_note_allocation] (
    [id]              bigint IDENTITY(1,1) NOT NULL,
    [amount]          decimal(14,2) NOT NULL,
    [created_at]      datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]      bigint NULL,
    [credit_note_id]  bigint NOT NULL,
    [invoice_id]      bigint NOT NULL,
    [is_deleted]      bit NOT NULL,
    [updated_at]      datetimeoffset NULL,
    [updated_by]      bigint NULL,
    CONSTRAINT [PK_credit_note_allocation] PRIMARY KEY ([id]),
    CONSTRAINT [FK_credit_note_allocation_credit_note_id] FOREIGN KEY ([credit_note_id]) REFERENCES [credit_note]([id]),
    CONSTRAINT [FK_credit_note_allocation_invoice_id] FOREIGN KEY ([invoice_id]) REFERENCES [invoice]([id])
);
GO

-- customer_refund  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[customer_refund] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [amount]             decimal(14,2) NOT NULL,
    [branch_id]          bigint NOT NULL,
    [cancel_reason]      nvarchar(500) NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [is_deleted]         bit NOT NULL,
    [journal_entry_id]   bigint NULL,
    [payment_method_id]  bigint NULL,
    [reason]             nvarchar(500) NOT NULL,
    [reference_no]       nvarchar(100) NULL,
    [refund_date]        date NOT NULL,
    [refund_no]          nvarchar(30) NOT NULL,
    [status]             nvarchar(20) NOT NULL,
    [tenant_id]          bigint NOT NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    CONSTRAINT [PK_customer_refund] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_customer_refund_refund_no_ux_customer_refund_no] UNIQUE ([refund_no], [ux_customer_refund_no]),
    CONSTRAINT [FK_customer_refund_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_customer_refund_journal_entry_id] FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry]([id]),
    CONSTRAINT [FK_customer_refund_payment_method_id] FOREIGN KEY ([payment_method_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_customer_refund_tenant_id] FOREIGN KEY ([tenant_id]) REFERENCES [tenant]([id])
);
GO

-- customer_refund_source  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[customer_refund_source] (
    [id]              bigint IDENTITY(1,1) NOT NULL,
    [amount]          decimal(14,2) NOT NULL,
    [created_at]      datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]      bigint NULL,
    [credit_note_id]  bigint NULL,
    [is_deleted]      bit NOT NULL,
    [payment_id]      bigint NULL,
    [refund_id]       bigint NOT NULL,
    [updated_at]      datetimeoffset NULL,
    [updated_by]      bigint NULL,
    CONSTRAINT [PK_customer_refund_source] PRIMARY KEY ([id]),
    CONSTRAINT [FK_customer_refund_source_credit_note_id] FOREIGN KEY ([credit_note_id]) REFERENCES [credit_note]([id]),
    CONSTRAINT [FK_customer_refund_source_payment_id] FOREIGN KEY ([payment_id]) REFERENCES [payment]([id]),
    CONSTRAINT [FK_customer_refund_source_refund_id] FOREIGN KEY ([refund_id]) REFERENCES [customer_refund]([id])
);
GO

-- voucher  (created in migration fullsp, 18 Oct)
CREATE TABLE [dbo].[voucher] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [account_id]         bigint NOT NULL,
    [amount]             decimal(18,2) NOT NULL,
    [bank_name]          nvarchar(100) NULL,
    [branch_id]          bigint NOT NULL,
    [building_id]        bigint NULL,
    [cancel_reason]      nvarchar(300) NULL,
    [cheque_date]        date NULL,
    [cheque_no]          nvarchar(50) NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [description]        nvarchar(500) NULL,
    [is_deleted]         bit NOT NULL,
    [journal_entry_id]   bigint NULL,
    [party_name]         nvarchar(200) NOT NULL,
    [payment_method_id]  bigint NULL,
    [reference_no]       nvarchar(100) NULL,
    [status]             nvarchar(20) NOT NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    [voucher_date]       date NOT NULL,
    [voucher_no]         nvarchar(30) NOT NULL,
    [voucher_type]       nvarchar(10) NOT NULL,
    CONSTRAINT [PK_voucher] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_voucher_voucher_no] UNIQUE ([voucher_no]),
    CONSTRAINT [FK_voucher_account_id] FOREIGN KEY ([account_id]) REFERENCES [account]([id]),
    CONSTRAINT [FK_voucher_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_voucher_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_voucher_journal_entry_id] FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry]([id]),
    CONSTRAINT [FK_voucher_payment_method_id] FOREIGN KEY ([payment_method_id]) REFERENCES [lookup_value]([id])
);
GO


-- =====================================================
-- Purchasing & supplier payments
-- =====================================================
-- vendor  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[vendor] (
    [id]                  bigint IDENTITY(1,1) NOT NULL,
    [address]             nvarchar(max) NULL,
    [branch_id]           bigint NULL,
    [code]                nvarchar(30) NULL,
    [created_at]          datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]          bigint NULL,
    [email]               nvarchar(150) NULL,
    [is_active]           bit NOT NULL,
    [is_deleted]          bit NOT NULL,
    [name_ar]             nvarchar(200) NULL,
    [name_en]             nvarchar(200) NULL,
    [payable_account_id]  bigint NULL,
    [phone]               nvarchar(30) NULL,
    [trade_license_no]    nvarchar(50) NULL,
    [trn]                 nvarchar(20) NULL,
    [updated_at]          datetimeoffset NULL,
    [updated_by]          bigint NULL,
    [vendor_type_id]      bigint NOT NULL,
    CONSTRAINT [PK_vendor] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_vendor_code] UNIQUE ([code]),
    CONSTRAINT [FK_vendor_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_vendor_payable_account_id] FOREIGN KEY ([payable_account_id]) REFERENCES [account]([id]),
    CONSTRAINT [FK_vendor_vendor_type_id] FOREIGN KEY ([vendor_type_id]) REFERENCES [lookup_value]([id])
);
GO

-- vendor_contact  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[vendor_contact] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [email]       nvarchar(150) NULL,
    [is_active]   bit NOT NULL,
    [is_default]  bit NOT NULL,
    [is_deleted]  bit NOT NULL,
    [name_ar]     nvarchar(150) NULL,
    [name_en]     nvarchar(150) NULL,
    [phone]       nvarchar(30) NULL,
    [position]    nvarchar(100) NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    [vendor_id]   bigint NOT NULL,
    CONSTRAINT [PK_vendor_contact] PRIMARY KEY ([id]),
    CONSTRAINT [FK_vendor_contact_vendor_id] FOREIGN KEY ([vendor_id]) REFERENCES [vendor]([id])
);
GO

-- vendor_bill  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[vendor_bill] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [bill_date]         date NOT NULL,
    [bill_no]           nvarchar(50) NOT NULL,
    [branch_id]         bigint NOT NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [currency_id]       bigint NULL,
    [due_date]          date NULL,
    [exchange_rate]     decimal(18,6) NULL,
    [is_deleted]        bit NOT NULL,
    [journal_entry_id]  bigint NULL,
    [notes]             nvarchar(max) NULL,
    [source_id]         bigint NULL,
    [source_type]       nvarchar(30) NULL,
    [status_id]         bigint NULL,
    [subtotal]          decimal(14,2) NULL,
    [total_amount]      decimal(14,2) NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    [vat_amount]        decimal(14,2) NULL,
    [vendor_id]         bigint NOT NULL,
    CONSTRAINT [PK_vendor_bill] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_vendor_bill_vendor_id_bill_no] UNIQUE ([vendor_id], [bill_no]),
    CONSTRAINT [FK_vendor_bill_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_vendor_bill_currency_id] FOREIGN KEY ([currency_id]) REFERENCES [currency]([id]),
    CONSTRAINT [FK_vendor_bill_journal_entry_id] FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry]([id]),
    CONSTRAINT [FK_vendor_bill_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_vendor_bill_vendor_id] FOREIGN KEY ([vendor_id]) REFERENCES [vendor]([id])
);
GO

-- vendor_bill_item  (created in migration InitialCreate, 17 Oct)
-- columns added later: tax_rate_id (allsp 19 Oct)
CREATE TABLE [dbo].[vendor_bill_item] (
    [id]                  bigint IDENTITY(1,1) NOT NULL,
    [amount]              decimal(14,2) NULL,
    [bill_id]             bigint NOT NULL,
    [created_at]          datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]          bigint NULL,
    [description]         nvarchar(300) NULL,
    [expense_account_id]  bigint NULL,
    [is_deleted]          bit NOT NULL,
    [item_type_id]        bigint NULL,
    [quantity]            decimal(14,3) NULL,
    [tax_rate_id]         bigint NULL,
    [unit_price]          decimal(14,4) NULL,
    [uom]                 nvarchar(10) NULL,
    [updated_at]          datetimeoffset NULL,
    [updated_by]          bigint NULL,
    [vat_rate]            decimal(5,2) NULL,
    CONSTRAINT [PK_vendor_bill_item] PRIMARY KEY ([id]),
    CONSTRAINT [FK_vendor_bill_item_bill_id] FOREIGN KEY ([bill_id]) REFERENCES [vendor_bill]([id]),
    CONSTRAINT [FK_vendor_bill_item_expense_account_id] FOREIGN KEY ([expense_account_id]) REFERENCES [account]([id]),
    CONSTRAINT [FK_vendor_bill_item_item_type_id] FOREIGN KEY ([item_type_id]) REFERENCES [lookup_value]([id])
);
GO

-- vendor_payment  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[vendor_payment] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [amount]             decimal(14,2) NOT NULL,
    [branch_id]          bigint NOT NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [currency_id]        bigint NULL,
    [is_deleted]         bit NOT NULL,
    [journal_entry_id]   bigint NULL,
    [notes]              nvarchar(max) NULL,
    [paid_by]            bigint NULL,
    [payment_date]       date NOT NULL,
    [payment_method_id]  bigint NULL,
    [payment_no]         nvarchar(30) NOT NULL,
    [reference_no]       nvarchar(100) NULL,
    [status_id]          bigint NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    [vendor_id]          bigint NOT NULL,
    CONSTRAINT [PK_vendor_payment] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_vendor_payment_payment_no] UNIQUE ([payment_no]),
    CONSTRAINT [FK_vendor_payment_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_vendor_payment_currency_id] FOREIGN KEY ([currency_id]) REFERENCES [currency]([id]),
    CONSTRAINT [FK_vendor_payment_journal_entry_id] FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry]([id]),
    CONSTRAINT [FK_vendor_payment_paid_by] FOREIGN KEY ([paid_by]) REFERENCES [app_user]([id]),
    CONSTRAINT [FK_vendor_payment_payment_method_id] FOREIGN KEY ([payment_method_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_vendor_payment_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_vendor_payment_vendor_id] FOREIGN KEY ([vendor_id]) REFERENCES [vendor]([id])
);
GO

-- vendor_payment_allocation  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[vendor_payment_allocation] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [amount]             decimal(14,2) NOT NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [is_deleted]         bit NOT NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    [vendor_bill_id]     bigint NOT NULL,
    [vendor_payment_id]  bigint NOT NULL,
    CONSTRAINT [PK_vendor_payment_allocation] PRIMARY KEY ([id]),
    CONSTRAINT [FK_vendor_payment_allocation_vendor_bill_id] FOREIGN KEY ([vendor_bill_id]) REFERENCES [vendor_bill]([id]),
    CONSTRAINT [FK_vendor_payment_allocation_vendor_payment_id] FOREIGN KEY ([vendor_payment_id]) REFERENCES [vendor_payment]([id])
);
GO


-- =====================================================
-- Customers, owners & tenants
-- =====================================================
-- party  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[party] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [address]           nvarchar(max) NULL,
    [branch_id]         bigint NULL,
    [city_id]           bigint NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [emirates_id]       nvarchar(20) NULL,
    [is_active]         bit NOT NULL,
    [is_deleted]        bit NOT NULL,
    [name_ar]           nvarchar(200) NULL,
    [name_en]           nvarchar(200) NULL,
    [nationality_id]    bigint NULL,
    [party_type_id]     bigint NOT NULL,
    [po_box]            nvarchar(20) NULL,
    [tax_number]        nvarchar(30) NULL,
    [trade_license_no]  nvarchar(50) NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    CONSTRAINT [PK_party] PRIMARY KEY ([id]),
    CONSTRAINT [FK_party_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_party_city_id] FOREIGN KEY ([city_id]) REFERENCES [city]([id]),
    CONSTRAINT [FK_party_nationality_id] FOREIGN KEY ([nationality_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_party_party_type_id] FOREIGN KEY ([party_type_id]) REFERENCES [lookup_value]([id])
);
GO

-- party_contact  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[party_contact] (
    [id]               bigint IDENTITY(1,1) NOT NULL,
    [contact_type_id]  bigint NOT NULL,
    [created_at]       datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]       bigint NULL,
    [is_active]        bit NOT NULL,
    [is_default]       bit NOT NULL,
    [is_deleted]       bit NOT NULL,
    [party_id]         bigint NOT NULL,
    [updated_at]       datetimeoffset NULL,
    [updated_by]       bigint NULL,
    [value]            nvarchar(150) NOT NULL,
    CONSTRAINT [PK_party_contact] PRIMARY KEY ([id]),
    CONSTRAINT [FK_party_contact_contact_type_id] FOREIGN KEY ([contact_type_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_party_contact_party_id] FOREIGN KEY ([party_id]) REFERENCES [party]([id])
);
GO

-- party_bank_account  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[party_bank_account] (
    [id]            bigint IDENTITY(1,1) NOT NULL,
    [account_name]  nvarchar(150) NULL,
    [bank_name]     nvarchar(150) NULL,
    [created_at]    datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]    bigint NULL,
    [iban]          nvarchar(34) NULL,
    [is_active]     bit NOT NULL,
    [is_default]    bit NOT NULL,
    [is_deleted]    bit NOT NULL,
    [party_id]      bigint NOT NULL,
    [updated_at]    datetimeoffset NULL,
    [updated_by]    bigint NULL,
    CONSTRAINT [PK_party_bank_account] PRIMARY KEY ([id]),
    CONSTRAINT [FK_party_bank_account_party_id] FOREIGN KEY ([party_id]) REFERENCES [party]([id])
);
GO

-- building_owner  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[building_owner] (
    [id]                    bigint IDENTITY(1,1) NOT NULL,
    [building_id]           bigint NOT NULL,
    [created_at]            datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]            bigint NULL,
    [from_date]             date NULL,
    [is_deleted]            bit NOT NULL,
    [is_primary]            bit NOT NULL,
    [ownership_percentage]  decimal(5,2) NULL,
    [party_id]              bigint NOT NULL,
    [to_date]               date NULL,
    [updated_at]            datetimeoffset NULL,
    [updated_by]            bigint NULL,
    CONSTRAINT [PK_building_owner] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_building_owner_building_id] UNIQUE ([building_id]),
    CONSTRAINT [FK_building_owner_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_building_owner_party_id] FOREIGN KEY ([party_id]) REFERENCES [party]([id])
);
GO

-- tenant  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[tenant] (
    [id]                     bigint IDENTITY(1,1) NOT NULL,
    [address]                nvarchar(max) NULL,
    [branch_id]              bigint NOT NULL,
    [city_id]                bigint NULL,
    [created_at]             datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]             bigint NULL,
    [date_of_birth]          date NULL,
    [email]                  nvarchar(150) NULL,
    [emirates_id]            nvarchar(20) NULL,
    [is_active]              bit NOT NULL,
    [is_deleted]             bit NOT NULL,
    [name_ar]                nvarchar(200) NULL,
    [name_en]                nvarchar(200) NULL,
    [nationality_id]         bigint NULL,
    [phone]                  nvarchar(30) NULL,
    [receivable_account_id]  bigint NULL,
    [tenant_code]            nvarchar(20) NOT NULL,
    [updated_at]             datetimeoffset NULL,
    [updated_by]             bigint NULL,
    CONSTRAINT [PK_tenant] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_tenant_emirates_id] UNIQUE ([emirates_id]),
    CONSTRAINT [UQ_tenant_tenant_code] UNIQUE ([tenant_code]),
    CONSTRAINT [FK_tenant_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_tenant_city_id] FOREIGN KEY ([city_id]) REFERENCES [city]([id]),
    CONSTRAINT [FK_tenant_nationality_id] FOREIGN KEY ([nationality_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_tenant_receivable_account_id] FOREIGN KEY ([receivable_account_id]) REFERENCES [account]([id])
);
GO


-- =====================================================
-- Buildings & units
-- =====================================================
-- building  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[building] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [address]           nvarchar(max) NULL,
    [branch_id]         bigint NOT NULL,
    [building_type_id]  bigint NULL,
    [city_id]           bigint NULL,
    [code]              nvarchar(20) NOT NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [is_active]         bit NOT NULL,
    [is_deleted]        bit NOT NULL,
    [latitude]          decimal(9,6) NULL,
    [longitude]         decimal(9,6) NULL,
    [name_ar]           nvarchar(200) NULL,
    [name_en]           nvarchar(200) NULL,
    [notes]             nvarchar(max) NULL,
    [po_box]            nvarchar(20) NULL,
    [status_id]         bigint NULL,
    [street]            nvarchar(150) NULL,
    [total_floors]      int NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    [zone]              nvarchar(100) NULL,
    CONSTRAINT [PK_building] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_building_code] UNIQUE ([code]),
    CONSTRAINT [FK_building_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_building_building_type_id] FOREIGN KEY ([building_type_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_building_city_id] FOREIGN KEY ([city_id]) REFERENCES [city]([id]),
    CONSTRAINT [FK_building_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id])
);
GO

-- unit  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[unit] (
    [id]                    bigint IDENTITY(1,1) NOT NULL,
    [area_sqm]              decimal(10,2) NULL,
    [bathrooms]             smallint NULL,
    [bedrooms]              smallint NULL,
    [building_id]           bigint NOT NULL,
    [created_at]            datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]            bigint NULL,
    [floor_no]              int NULL,
    [is_active]             bit NOT NULL,
    [is_deleted]            bit NOT NULL,
    [last_inspection_date]  date NULL,
    [notes]                 nvarchar(max) NULL,
    [rent_amount]           decimal(14,2) NULL,
    [status_id]             bigint NULL,
    [unit_no]               nvarchar(20) NOT NULL,
    [unit_type_id]          bigint NULL,
    [updated_at]            datetimeoffset NULL,
    [updated_by]            bigint NULL,
    CONSTRAINT [PK_unit] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_unit_building_id_unit_no] UNIQUE ([building_id], [unit_no]),
    CONSTRAINT [FK_unit_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_unit_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_unit_unit_type_id] FOREIGN KEY ([unit_type_id]) REFERENCES [lookup_value]([id])
);
GO


-- =====================================================
-- Gas supply contracts
-- =====================================================
-- supply_contract  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[supply_contract] (
    [id]                     bigint IDENTITY(1,1) NOT NULL,
    [auto_renew]             bit NOT NULL,
    [branch_id]              bigint NOT NULL,
    [contract_no]            nvarchar(30) NOT NULL,
    [created_at]             datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]             bigint NULL,
    [deposit_amount]         decimal(14,2) NULL,
    [deposit_date]           date NULL,
    [deposit_paid]           bit NOT NULL,
    [end_date]               date NULL,
    [is_active]              bit NOT NULL,
    [is_deleted]             bit NOT NULL,
    [renewal_period_months]  smallint NULL,
    [start_date]             date NOT NULL,
    [status_id]              bigint NULL,
    [tenant_id]              bigint NOT NULL,
    [terminated_at]          date NULL,
    [terms]                  nvarchar(max) NULL,
    [unit_id]                bigint NOT NULL,
    [updated_at]             datetimeoffset NULL,
    [updated_by]             bigint NULL,
    CONSTRAINT [PK_supply_contract] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_supply_contract_contract_no] UNIQUE ([contract_no]),
    CONSTRAINT [UQ_supply_contract_unit_id] UNIQUE ([unit_id]),
    CONSTRAINT [FK_supply_contract_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_supply_contract_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_supply_contract_tenant_id] FOREIGN KEY ([tenant_id]) REFERENCES [tenant]([id]),
    CONSTRAINT [FK_supply_contract_unit_id] FOREIGN KEY ([unit_id]) REFERENCES [unit]([id])
);
GO

-- contract_fee  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[contract_fee] (
    [id]              bigint IDENTITY(1,1) NOT NULL,
    [amount]          decimal(14,4) NOT NULL,
    [contract_id]     bigint NOT NULL,
    [created_at]      datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]      bigint NULL,
    [currency_id]     bigint NULL,
    [cycle_id]        bigint NOT NULL,
    [effective_from]  date NOT NULL,
    [effective_to]    date NULL,
    [fee_type_id]     bigint NOT NULL,
    [is_active]       bit NOT NULL,
    [is_deleted]      bit NOT NULL,
    [notes]           nvarchar(max) NULL,
    [updated_at]      datetimeoffset NULL,
    [updated_by]      bigint NULL,
    CONSTRAINT [PK_contract_fee] PRIMARY KEY ([id]),
    CONSTRAINT [FK_contract_fee_contract_id] FOREIGN KEY ([contract_id]) REFERENCES [supply_contract]([id]),
    CONSTRAINT [FK_contract_fee_currency_id] FOREIGN KEY ([currency_id]) REFERENCES [currency]([id]),
    CONSTRAINT [FK_contract_fee_cycle_id] FOREIGN KEY ([cycle_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_contract_fee_fee_type_id] FOREIGN KEY ([fee_type_id]) REFERENCES [lookup_value]([id])
);
GO

-- contract_term  (created in migration updatetermcondtion, 18 Oct)
CREATE TABLE [dbo].[contract_term] (
    [id]             bigint IDENTITY(1,1) NOT NULL,
    [category]       nvarchar(30) NOT NULL,
    [contract_id]    bigint NULL,
    [contract_type]  nvarchar(20) NOT NULL,
    [created_at]     datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]     bigint NULL,
    [is_active]      bit NOT NULL,
    [is_custom]      bit NOT NULL,
    [is_default]     bit NOT NULL,
    [is_deleted]     bit NOT NULL,
    [is_modified]    bit NOT NULL,
    [sort_order]     int NOT NULL,
    [source_id]      bigint NULL,
    [text_ar]        nvarchar(max) NOT NULL,
    [text_en]        nvarchar(max) NULL,
    [updated_at]     datetimeoffset NULL,
    [updated_by]     bigint NULL,
    CONSTRAINT [PK_contract_term] PRIMARY KEY ([id]),
    CONSTRAINT [FK_contract_term_source_id] FOREIGN KEY ([source_id]) REFERENCES [contract_term]([id])
);
GO


-- =====================================================
-- Meters & service requests
-- =====================================================
-- meter  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[meter] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [branch_id]         bigint NOT NULL,
    [brand]             nvarchar(50) NULL,
    [calibration_date]  date NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [initial_reading]   decimal(14,3) NULL,
    [install_date]      date NULL,
    [is_active]         bit NOT NULL,
    [is_deleted]        bit NOT NULL,
    [location_note]     nvarchar(200) NULL,
    [meter_no]          nvarchar(20) NOT NULL,
    [meter_type_id]     bigint NULL,
    [model]             nvarchar(50) NULL,
    [serial_no]         nvarchar(50) NOT NULL,
    [sim_card_no]       nvarchar(30) NULL,
    [status_id]         bigint NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    CONSTRAINT [PK_meter] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_meter_meter_no] UNIQUE ([meter_no]),
    CONSTRAINT [UQ_meter_serial_no] UNIQUE ([serial_no]),
    CONSTRAINT [FK_meter_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_meter_meter_type_id] FOREIGN KEY ([meter_type_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_meter_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id])
);
GO

-- meter_assignment  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[meter_assignment] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [from_date]   date NOT NULL,
    [is_deleted]  bit NOT NULL,
    [meter_id]    bigint NOT NULL,
    [to_date]     date NULL,
    [unit_id]     bigint NOT NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    CONSTRAINT [PK_meter_assignment] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_meter_assignment_meter_id] UNIQUE ([meter_id]),
    CONSTRAINT [UQ_meter_assignment_unit_id] UNIQUE ([unit_id]),
    CONSTRAINT [FK_meter_assignment_meter_id] FOREIGN KEY ([meter_id]) REFERENCES [meter]([id]),
    CONSTRAINT [FK_meter_assignment_unit_id] FOREIGN KEY ([unit_id]) REFERENCES [unit]([id])
);
GO

-- meter_reading  (created in migration InitialCreate, 17 Oct)
-- columns added later: contract_id (x 18 Oct), reading_type_id (x 18 Oct), unit_id (x 18 Oct)
CREATE TABLE [dbo].[meter_reading] (
    [id]               bigint IDENTITY(1,1) NOT NULL,
    [consumption]      decimal(14,3) NULL,
    [contract_id]      bigint NULL,
    [created_at]       datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]       bigint NULL,
    [invoice_id]       bigint NULL,
    [is_deleted]       bit NOT NULL,
    [meter_id]         bigint NOT NULL,
    [method_id]        bigint NULL,
    [notes]            nvarchar(max) NULL,
    [read_by]          bigint NULL,
    [reading_date]     datetimeoffset NOT NULL,
    [reading_type_id]  bigint NULL,
    [reading_value]    decimal(14,3) NOT NULL,
    [unit_id]          bigint NULL,
    [updated_at]       datetimeoffset NULL,
    [updated_by]       bigint NULL,
    CONSTRAINT [PK_meter_reading] PRIMARY KEY ([id]),
    CONSTRAINT [FK_meter_reading_contract_id] FOREIGN KEY ([contract_id]) REFERENCES [supply_contract]([id]),
    CONSTRAINT [FK_meter_reading_invoice_id] FOREIGN KEY ([invoice_id]) REFERENCES [invoice]([id]),
    CONSTRAINT [FK_meter_reading_meter_id] FOREIGN KEY ([meter_id]) REFERENCES [meter]([id]),
    CONSTRAINT [FK_meter_reading_method_id] FOREIGN KEY ([method_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_meter_reading_read_by] FOREIGN KEY ([read_by]) REFERENCES [app_user]([id]),
    CONSTRAINT [FK_meter_reading_reading_type_id] FOREIGN KEY ([reading_type_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_meter_reading_unit_id] FOREIGN KEY ([unit_id]) REFERENCES [unit]([id])
);
GO

-- meter_topup  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[meter_topup] (
    [id]           bigint IDENTITY(1,1) NOT NULL,
    [amount]       decimal(14,2) NOT NULL,
    [created_at]   datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]   bigint NULL,
    [is_deleted]   bit NOT NULL,
    [meter_id]     bigint NOT NULL,
    [payment_id]   bigint NULL,
    [quantity_m3]  decimal(14,3) NULL,
    [topup_date]   datetimeoffset NOT NULL,
    [updated_at]   datetimeoffset NULL,
    [updated_by]   bigint NULL,
    CONSTRAINT [PK_meter_topup] PRIMARY KEY ([id]),
    CONSTRAINT [FK_meter_topup_meter_id] FOREIGN KEY ([meter_id]) REFERENCES [meter]([id]),
    CONSTRAINT [FK_meter_topup_payment_id] FOREIGN KEY ([payment_id]) REFERENCES [payment]([id])
);
GO

-- meter_service  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[meter_service] (
    [id]                   bigint IDENTITY(1,1) NOT NULL,
    [after_reading]        decimal(14,3) NULL,
    [before_reading]       decimal(14,3) NULL,
    [branch_id]            bigint NOT NULL,
    [completed_date]       date NULL,
    [contract_id]          bigint NULL,
    [created_at]           datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]           bigint NULL,
    [fee_invoice_item_id]  bigint NULL,
    [is_deleted]           bit NOT NULL,
    [issue_type_id]        bigint NULL,
    [meter_id]             bigint NOT NULL,
    [new_meter_id]         bigint NULL,
    [priority_id]          bigint NULL,
    [reason]               nvarchar(max) NULL,
    [requested_date]       date NULL,
    [safety_check_passed]  bit NULL,
    [service_no]           nvarchar(30) NULL,
    [service_type_id]      bigint NOT NULL,
    [status_id]            bigint NULL,
    [technician_id]        bigint NULL,
    [tenant_notified]      bit NULL,
    [updated_at]           datetimeoffset NULL,
    [updated_by]           bigint NULL,
    CONSTRAINT [PK_meter_service] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_meter_service_service_no] UNIQUE ([service_no]),
    CONSTRAINT [FK_meter_service_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_meter_service_contract_id] FOREIGN KEY ([contract_id]) REFERENCES [supply_contract]([id]),
    CONSTRAINT [FK_meter_service_fee_invoice_item_id] FOREIGN KEY ([fee_invoice_item_id]) REFERENCES [invoice_item]([id]),
    CONSTRAINT [FK_meter_service_issue_type_id] FOREIGN KEY ([issue_type_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_meter_service_meter_id] FOREIGN KEY ([meter_id]) REFERENCES [meter]([id]),
    CONSTRAINT [FK_meter_service_new_meter_id] FOREIGN KEY ([new_meter_id]) REFERENCES [meter]([id]),
    CONSTRAINT [FK_meter_service_priority_id] FOREIGN KEY ([priority_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_meter_service_service_type_id] FOREIGN KEY ([service_type_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_meter_service_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_meter_service_technician_id] FOREIGN KEY ([technician_id]) REFERENCES [app_user]([id])
);
GO

-- service_request  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[service_request] (
    [id]                  bigint IDENTITY(1,1) NOT NULL,
    [assigned_to]         bigint NULL,
    [assigned_vendor_id]  bigint NULL,
    [branch_id]           bigint NOT NULL,
    [building_id]         bigint NOT NULL,
    [closed_at]           datetimeoffset NULL,
    [created_at]          datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]          bigint NULL,
    [description]         nvarchar(max) NOT NULL,
    [is_deleted]          bit NOT NULL,
    [meter_id]            bigint NULL,
    [notes]               nvarchar(max) NULL,
    [priority_id]         bigint NULL,
    [request_no]          nvarchar(30) NOT NULL,
    [requested_at]        datetimeoffset NULL,
    [status_id]           bigint NULL,
    [tenant_id]           bigint NULL,
    [unit_id]             bigint NULL,
    [updated_at]          datetimeoffset NULL,
    [updated_by]          bigint NULL,
    CONSTRAINT [PK_service_request] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_service_request_request_no] UNIQUE ([request_no]),
    CONSTRAINT [FK_service_request_assigned_to] FOREIGN KEY ([assigned_to]) REFERENCES [app_user]([id]),
    CONSTRAINT [FK_service_request_assigned_vendor_id] FOREIGN KEY ([assigned_vendor_id]) REFERENCES [vendor]([id]),
    CONSTRAINT [FK_service_request_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_service_request_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_service_request_meter_id] FOREIGN KEY ([meter_id]) REFERENCES [meter]([id]),
    CONSTRAINT [FK_service_request_priority_id] FOREIGN KEY ([priority_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_service_request_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_service_request_tenant_id] FOREIGN KEY ([tenant_id]) REFERENCES [tenant]([id]),
    CONSTRAINT [FK_service_request_unit_id] FOREIGN KEY ([unit_id]) REFERENCES [unit]([id])
);
GO


-- =====================================================
-- Tanks & refills
-- =====================================================
-- gas_type  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[gas_type] (
    [id]           bigint IDENTITY(1,1) NOT NULL,
    [code]         nvarchar(20) NOT NULL,
    [created_at]   datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]   bigint NULL,
    [is_active]    bit NOT NULL,
    [is_deleted]   bit NOT NULL,
    [litre_to_m3]  decimal(18,6) NOT NULL,
    [name_ar]      nvarchar(100) NULL,
    [name_en]      nvarchar(100) NULL,
    [updated_at]   datetimeoffset NULL,
    [updated_by]   bigint NULL,
    CONSTRAINT [PK_gas_type] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_gas_type_code] UNIQUE ([code])
);
GO

-- tank  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[tank] (
    [id]                    bigint IDENTITY(1,1) NOT NULL,
    [branch_id]             bigint NOT NULL,
    [building_id]           bigint NOT NULL,
    [capacity_litres]       decimal(14,3) NOT NULL,
    [created_at]            datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]            bigint NULL,
    [current_level_litres]  decimal(14,3) NULL,
    [gas_type_id]           bigint NOT NULL,
    [install_date]          date NULL,
    [is_active]             bit NOT NULL,
    [is_deleted]            bit NOT NULL,
    [last_inspection_date]  date NULL,
    [manufacturer]          nvarchar(100) NULL,
    [next_inspection_date]  date NULL,
    [serial_no]             nvarchar(50) NULL,
    [status_id]             bigint NULL,
    [tank_no]               nvarchar(20) NOT NULL,
    [updated_at]            datetimeoffset NULL,
    [updated_by]            bigint NULL,
    CONSTRAINT [PK_tank] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_tank_building_id_tank_no] UNIQUE ([building_id], [tank_no]),
    CONSTRAINT [FK_tank_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_tank_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_tank_gas_type_id] FOREIGN KEY ([gas_type_id]) REFERENCES [gas_type]([id]),
    CONSTRAINT [FK_tank_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id])
);
GO

-- tank_level_history  (created in migration InitialCreate, 17 Oct)
-- columns added later: read_by (all 18 Oct)
CREATE TABLE [dbo].[tank_level_history] (
    [id]              bigint IDENTITY(1,1) NOT NULL,
    [created_at]      datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]      bigint NULL,
    [event_date]      datetimeoffset NOT NULL,
    [event_type_id]   bigint NULL,
    [is_deleted]      bit NOT NULL,
    [level_litres]    decimal(14,3) NOT NULL,
    [notes]           nvarchar(max) NULL,
    [read_by]         nvarchar(100) NULL,
    [tank_id]         bigint NOT NULL,
    [tank_refill_id]  bigint NULL,
    [updated_at]      datetimeoffset NULL,
    [updated_by]      bigint NULL,
    CONSTRAINT [PK_tank_level_history] PRIMARY KEY ([id]),
    CONSTRAINT [FK_tank_level_history_event_type_id] FOREIGN KEY ([event_type_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_tank_level_history_tank_id] FOREIGN KEY ([tank_id]) REFERENCES [tank]([id]),
    CONSTRAINT [FK_tank_level_history_tank_refill_id] FOREIGN KEY ([tank_refill_id]) REFERENCES [tank_refill]([id])
);
GO

-- tank_refill  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[tank_refill] (
    [id]                    bigint IDENTITY(1,1) NOT NULL,
    [created_at]            datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]            bigint NULL,
    [delivered_qty_litres]  decimal(14,3) NULL,
    [driver_id]             bigint NULL,
    [is_deleted]            bit NOT NULL,
    [level_after_litres]    decimal(14,3) NULL,
    [level_before_litres]   decimal(14,3) NULL,
    [notes]                 nvarchar(max) NULL,
    [refill_datetime]       datetimeoffset NOT NULL,
    [refill_request_id]     bigint NOT NULL,
    [safety_check_passed]   bit NULL,
    [tank_id]               bigint NOT NULL,
    [updated_at]            datetimeoffset NULL,
    [updated_by]            bigint NULL,
    [vehicle_id]            bigint NULL,
    CONSTRAINT [PK_tank_refill] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_tank_refill_refill_request_id] UNIQUE ([refill_request_id]),
    CONSTRAINT [FK_tank_refill_driver_id] FOREIGN KEY ([driver_id]) REFERENCES [driver]([id]),
    CONSTRAINT [FK_tank_refill_refill_request_id] FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request]([id]),
    CONSTRAINT [FK_tank_refill_tank_id] FOREIGN KEY ([tank_id]) REFERENCES [tank]([id]),
    CONSTRAINT [FK_tank_refill_vehicle_id] FOREIGN KEY ([vehicle_id]) REFERENCES [vehicle]([id])
);
GO

-- refill_schedule  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[refill_schedule] (
    [id]                       bigint IDENTITY(1,1) NOT NULL,
    [civil_defense_confirmed]  bit NOT NULL,
    [civil_defense_ref]        nvarchar(50) NULL,
    [created_at]               datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]               bigint NULL,
    [delivery_date]            date NOT NULL,
    [is_deleted]               bit NOT NULL,
    [refill_request_id]        bigint NOT NULL,
    [time_slot]                nvarchar(30) NULL,
    [updated_at]               datetimeoffset NULL,
    [updated_by]               bigint NULL,
    CONSTRAINT [PK_refill_schedule] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_refill_schedule_refill_request_id] UNIQUE ([refill_request_id]),
    CONSTRAINT [FK_refill_schedule_refill_request_id] FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request]([id])
);
GO

-- refill_request  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[refill_request] (
    [id]                       bigint IDENTITY(1,1) NOT NULL,
    [approval_notes]           nvarchar(max) NULL,
    [approved_at]              datetimeoffset NULL,
    [approved_by]              bigint NULL,
    [branch_id]                bigint NOT NULL,
    [completed_at]             datetimeoffset NULL,
    [created_at]               datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]               bigint NULL,
    [current_stage_id]         bigint NULL,
    [is_deleted]               bit NOT NULL,
    [level_at_request_litres]  decimal(14,3) NULL,
    [notes]                    nvarchar(max) NULL,
    [priority_id]              bigint NULL,
    [request_date]             date NOT NULL,
    [request_no]               nvarchar(30) NOT NULL,
    [requested_by]             bigint NULL,
    [requested_qty_litres]     decimal(14,3) NOT NULL,
    [status_id]                bigint NULL,
    [tank_id]                  bigint NOT NULL,
    [updated_at]               datetimeoffset NULL,
    [updated_by]               bigint NULL,
    CONSTRAINT [PK_refill_request] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_refill_request_request_no] UNIQUE ([request_no]),
    CONSTRAINT [FK_refill_request_approved_by] FOREIGN KEY ([approved_by]) REFERENCES [app_user]([id]),
    CONSTRAINT [FK_refill_request_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_refill_request_current_stage_id] FOREIGN KEY ([current_stage_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_refill_request_priority_id] FOREIGN KEY ([priority_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_refill_request_requested_by] FOREIGN KEY ([requested_by]) REFERENCES [app_user]([id]),
    CONSTRAINT [FK_refill_request_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_refill_request_tank_id] FOREIGN KEY ([tank_id]) REFERENCES [tank]([id])
);
GO

-- refill_stage_history  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[refill_stage_history] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [changed_at]         datetimeoffset NOT NULL,
    [changed_by]         bigint NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [from_stage_id]      bigint NULL,
    [is_deleted]         bit NOT NULL,
    [refill_request_id]  bigint NOT NULL,
    [remarks]            nvarchar(max) NULL,
    [to_stage_id]        bigint NOT NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    CONSTRAINT [PK_refill_stage_history] PRIMARY KEY ([id]),
    CONSTRAINT [FK_refill_stage_history_changed_by] FOREIGN KEY ([changed_by]) REFERENCES [app_user]([id]),
    CONSTRAINT [FK_refill_stage_history_from_stage_id] FOREIGN KEY ([from_stage_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_refill_stage_history_refill_request_id] FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request]([id]),
    CONSTRAINT [FK_refill_stage_history_to_stage_id] FOREIGN KEY ([to_stage_id]) REFERENCES [lookup_value]([id])
);
GO

-- refill_po  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[refill_po] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [currency_id]        bigint NULL,
    [is_deleted]         bit NOT NULL,
    [po_date]            date NOT NULL,
    [po_number]          nvarchar(30) NOT NULL,
    [qty_litres]         decimal(14,3) NULL,
    [refill_request_id]  bigint NOT NULL,
    [total_amount]       decimal(14,2) NULL,
    [unit_price]         decimal(14,4) NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    [vendor_id]          bigint NOT NULL,
    CONSTRAINT [PK_refill_po] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_refill_po_po_number] UNIQUE ([po_number]),
    CONSTRAINT [UQ_refill_po_refill_request_id] UNIQUE ([refill_request_id]),
    CONSTRAINT [FK_refill_po_currency_id] FOREIGN KEY ([currency_id]) REFERENCES [currency]([id]),
    CONSTRAINT [FK_refill_po_refill_request_id] FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request]([id]),
    CONSTRAINT [FK_refill_po_vendor_id] FOREIGN KEY ([vendor_id]) REFERENCES [vendor]([id])
);
GO

-- refill_collection  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[refill_collection] (
    [id]                    bigint IDENTITY(1,1) NOT NULL,
    [collected_qty_litres]  decimal(14,3) NULL,
    [collection_date]       datetimeoffset NULL,
    [created_at]            datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]            bigint NULL,
    [driver_id]             bigint NULL,
    [is_deleted]            bit NOT NULL,
    [meter_after]           decimal(14,3) NULL,
    [meter_before]          decimal(14,3) NULL,
    [refill_request_id]     bigint NOT NULL,
    [updated_at]            datetimeoffset NULL,
    [updated_by]            bigint NULL,
    [vehicle_id]            bigint NULL,
    CONSTRAINT [PK_refill_collection] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_refill_collection_refill_request_id] UNIQUE ([refill_request_id]),
    CONSTRAINT [FK_refill_collection_driver_id] FOREIGN KEY ([driver_id]) REFERENCES [driver]([id]),
    CONSTRAINT [FK_refill_collection_refill_request_id] FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request]([id]),
    CONSTRAINT [FK_refill_collection_vehicle_id] FOREIGN KEY ([vehicle_id]) REFERENCES [vehicle]([id])
);
GO

-- refill_purchase_invoice  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[refill_purchase_invoice] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [is_deleted]         bit NOT NULL,
    [refill_request_id]  bigint NOT NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    [vendor_bill_id]     bigint NOT NULL,
    CONSTRAINT [PK_refill_purchase_invoice] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_refill_purchase_invoice_refill_request_id] UNIQUE ([refill_request_id]),
    CONSTRAINT [FK_refill_purchase_invoice_refill_request_id] FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request]([id]),
    CONSTRAINT [FK_refill_purchase_invoice_vendor_bill_id] FOREIGN KEY ([vendor_bill_id]) REFERENCES [vendor_bill]([id])
);
GO

-- refill_supplier_payment  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[refill_supplier_payment] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [is_deleted]         bit NOT NULL,
    [refill_request_id]  bigint NOT NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    [vendor_payment_id]  bigint NOT NULL,
    CONSTRAINT [PK_refill_supplier_payment] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_refill_supplier_payment_refill_request_id] UNIQUE ([refill_request_id]),
    CONSTRAINT [FK_refill_supplier_payment_refill_request_id] FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request]([id]),
    CONSTRAINT [FK_refill_supplier_payment_vendor_payment_id] FOREIGN KEY ([vendor_payment_id]) REFERENCES [vendor_payment]([id])
);
GO


-- =====================================================
-- AMC & maintenance visits
-- =====================================================
-- amc_contract  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[amc_contract] (
    [id]                   bigint IDENTITY(1,1) NOT NULL,
    [annual_value]         decimal(14,2) NULL,
    [branch_id]            bigint NOT NULL,
    [building_id]          bigint NOT NULL,
    [contract_no]          nvarchar(30) NOT NULL,
    [created_at]           datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]           bigint NULL,
    [currency_id]          bigint NULL,
    [emergency_included]   bit NOT NULL,
    [end_date]             date NOT NULL,
    [is_active]            bit NOT NULL,
    [is_deleted]           bit NOT NULL,
    [notes]                nvarchar(max) NULL,
    [owner_party_id]       bigint NULL,
    [response_time_hours]  smallint NULL,
    [start_date]           date NOT NULL,
    [status_id]            bigint NULL,
    [updated_at]           datetimeoffset NULL,
    [updated_by]           bigint NULL,
    [vendor_id]            bigint NOT NULL,
    [visit_frequency_id]   bigint NULL,
    [visits_total]         smallint NULL,
    CONSTRAINT [PK_amc_contract] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_amc_contract_contract_no] UNIQUE ([contract_no]),
    CONSTRAINT [FK_amc_contract_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_amc_contract_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_amc_contract_currency_id] FOREIGN KEY ([currency_id]) REFERENCES [currency]([id]),
    CONSTRAINT [FK_amc_contract_owner_party_id] FOREIGN KEY ([owner_party_id]) REFERENCES [party]([id]),
    CONSTRAINT [FK_amc_contract_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_amc_contract_vendor_id] FOREIGN KEY ([vendor_id]) REFERENCES [vendor]([id]),
    CONSTRAINT [FK_amc_contract_visit_frequency_id] FOREIGN KEY ([visit_frequency_id]) REFERENCES [lookup_value]([id])
);
GO

-- amc_scope_item  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[amc_scope_item] (
    [id]            bigint IDENTITY(1,1) NOT NULL,
    [contract_id]   bigint NOT NULL,
    [created_at]    datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]    bigint NULL,
    [description]   nvarchar(max) NOT NULL,
    [is_deleted]    bit NOT NULL,
    [item_type_id]  bigint NOT NULL,
    [updated_at]    datetimeoffset NULL,
    [updated_by]    bigint NULL,
    CONSTRAINT [PK_amc_scope_item] PRIMARY KEY ([id]),
    CONSTRAINT [FK_amc_scope_item_contract_id] FOREIGN KEY ([contract_id]) REFERENCES [amc_contract]([id]),
    CONSTRAINT [FK_amc_scope_item_item_type_id] FOREIGN KEY ([item_type_id]) REFERENCES [lookup_value]([id])
);
GO

-- amc_term  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[amc_term] (
    [id]            bigint IDENTITY(1,1) NOT NULL,
    [contract_id]   bigint NOT NULL,
    [created_at]    datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]    bigint NULL,
    [description]   nvarchar(max) NOT NULL,
    [is_custom]     bit NOT NULL,
    [is_deleted]    bit NOT NULL,
    [term_type_id]  bigint NULL,
    [updated_at]    datetimeoffset NULL,
    [updated_by]    bigint NULL,
    CONSTRAINT [PK_amc_term] PRIMARY KEY ([id]),
    CONSTRAINT [FK_amc_term_contract_id] FOREIGN KEY ([contract_id]) REFERENCES [amc_contract]([id]),
    CONSTRAINT [FK_amc_term_term_type_id] FOREIGN KEY ([term_type_id]) REFERENCES [lookup_value]([id])
);
GO

-- amc_payment_schedule  (created in migration InitialCreate, 17 Oct)
-- columns added later: invoice_id (allcontract 18 Oct)
CREATE TABLE [dbo].[amc_payment_schedule] (
    [id]              bigint IDENTITY(1,1) NOT NULL,
    [amount]          decimal(14,2) NOT NULL,
    [contract_id]     bigint NOT NULL,
    [created_at]      datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]      bigint NULL,
    [due_date]        date NOT NULL,
    [invoice_id]      bigint NULL,
    [is_deleted]      bit NOT NULL,
    [label]           nvarchar(100) NULL,
    [notes]           nvarchar(max) NULL,
    [status_id]       bigint NULL,
    [updated_at]      datetimeoffset NULL,
    [updated_by]      bigint NULL,
    [vendor_bill_id]  bigint NULL,
    CONSTRAINT [PK_amc_payment_schedule] PRIMARY KEY ([id]),
    CONSTRAINT [FK_amc_payment_schedule_contract_id] FOREIGN KEY ([contract_id]) REFERENCES [amc_contract]([id]),
    CONSTRAINT [FK_amc_payment_schedule_invoice_id] FOREIGN KEY ([invoice_id]) REFERENCES [invoice]([id]),
    CONSTRAINT [FK_amc_payment_schedule_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_amc_payment_schedule_vendor_bill_id] FOREIGN KEY ([vendor_bill_id]) REFERENCES [vendor_bill]([id])
);
GO

-- maintenance_visit  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[maintenance_visit] (
    [id]                 bigint IDENTITY(1,1) NOT NULL,
    [amc_contract_id]    bigint NOT NULL,
    [branch_id]          bigint NOT NULL,
    [building_id]        bigint NOT NULL,
    [completed_at]       datetimeoffset NULL,
    [created_at]         datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]         bigint NULL,
    [is_deleted]         bit NOT NULL,
    [notes]              nvarchar(max) NULL,
    [period_label]       nvarchar(20) NULL,
    [report_summary]     nvarchar(max) NULL,
    [scheduled_date]     date NOT NULL,
    [status_id]          bigint NULL,
    [technician_phone]   nvarchar(30) NULL,
    [time_slot]          nvarchar(30) NULL,
    [updated_at]         datetimeoffset NULL,
    [updated_by]         bigint NULL,
    [vendor_contact_id]  bigint NULL,
    [visit_date]         date NULL,
    [visit_no]           nvarchar(30) NULL,
    [visit_type_id]      bigint NULL,
    CONSTRAINT [PK_maintenance_visit] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_maintenance_visit_visit_no] UNIQUE ([visit_no]),
    CONSTRAINT [FK_maintenance_visit_amc_contract_id] FOREIGN KEY ([amc_contract_id]) REFERENCES [amc_contract]([id]),
    CONSTRAINT [FK_maintenance_visit_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_maintenance_visit_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_maintenance_visit_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_maintenance_visit_vendor_contact_id] FOREIGN KEY ([vendor_contact_id]) REFERENCES [vendor_contact]([id]),
    CONSTRAINT [FK_maintenance_visit_visit_type_id] FOREIGN KEY ([visit_type_id]) REFERENCES [lookup_value]([id])
);
GO

-- visit_checklist_item  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[visit_checklist_item] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [is_deleted]        bit NOT NULL,
    [is_done]           bit NOT NULL,
    [item_text]         nvarchar(300) NOT NULL,
    [notes]             nvarchar(max) NULL,
    [sort_order]        int NULL,
    [template_item_id]  bigint NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    [visit_id]          bigint NOT NULL,
    CONSTRAINT [PK_visit_checklist_item] PRIMARY KEY ([id]),
    CONSTRAINT [FK_visit_checklist_item_template_item_id] FOREIGN KEY ([template_item_id]) REFERENCES [checklist_template_item]([id]),
    CONSTRAINT [FK_visit_checklist_item_visit_id] FOREIGN KEY ([visit_id]) REFERENCES [maintenance_visit]([id])
);
GO

-- visit_finding  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[visit_finding] (
    [id]                  bigint IDENTITY(1,1) NOT NULL,
    [action_taken]        nvarchar(max) NULL,
    [created_at]          datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]          bigint NULL,
    [description]         nvarchar(max) NOT NULL,
    [is_deleted]          bit NOT NULL,
    [service_request_id]  bigint NULL,
    [severity_id]         bigint NULL,
    [updated_at]          datetimeoffset NULL,
    [updated_by]          bigint NULL,
    [visit_id]            bigint NOT NULL,
    CONSTRAINT [PK_visit_finding] PRIMARY KEY ([id]),
    CONSTRAINT [FK_visit_finding_service_request_id] FOREIGN KEY ([service_request_id]) REFERENCES [service_request]([id]),
    CONSTRAINT [FK_visit_finding_severity_id] FOREIGN KEY ([severity_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_visit_finding_visit_id] FOREIGN KEY ([visit_id]) REFERENCES [maintenance_visit]([id])
);
GO

-- checklist_template  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[checklist_template] (
    [id]             bigint IDENTITY(1,1) NOT NULL,
    [created_at]     datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]     bigint NULL,
    [is_active]      bit NOT NULL,
    [is_deleted]     bit NOT NULL,
    [name_ar]        nvarchar(150) NULL,
    [name_en]        nvarchar(150) NULL,
    [updated_at]     datetimeoffset NULL,
    [updated_by]     bigint NULL,
    [visit_type_id]  bigint NULL,
    CONSTRAINT [PK_checklist_template] PRIMARY KEY ([id]),
    CONSTRAINT [FK_checklist_template_visit_type_id] FOREIGN KEY ([visit_type_id]) REFERENCES [lookup_value]([id])
);
GO

-- checklist_template_item  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[checklist_template_item] (
    [id]            bigint IDENTITY(1,1) NOT NULL,
    [created_at]    datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]    bigint NULL,
    [is_deleted]    bit NOT NULL,
    [item_text_ar]  nvarchar(300) NULL,
    [item_text_en]  nvarchar(300) NULL,
    [sort_order]    int NULL,
    [template_id]   bigint NOT NULL,
    [updated_at]    datetimeoffset NULL,
    [updated_by]    bigint NULL,
    CONSTRAINT [PK_checklist_template_item] PRIMARY KEY ([id]),
    CONSTRAINT [FK_checklist_template_item_template_id] FOREIGN KEY ([template_id]) REFERENCES [checklist_template]([id])
);
GO


-- =====================================================
-- Civil defence certificates
-- =====================================================
-- cd_certificate  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[cd_certificate] (
    [id]               bigint IDENTITY(1,1) NOT NULL,
    [branch_id]        bigint NOT NULL,
    [building_id]      bigint NOT NULL,
    [cert_type_id]     bigint NULL,
    [certificate_no]   nvarchar(50) NULL,
    [created_at]       datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]       bigint NULL,
    [expiry_date]      date NULL,
    [inspection_date]  date NULL,
    [inspector_name]   nvarchar(150) NULL,
    [is_deleted]       bit NOT NULL,
    [issue_date]       date NULL,
    [issued_by]        nvarchar(150) NULL,
    [license_no]       nvarchar(50) NULL,
    [notes]            nvarchar(max) NULL,
    [scope]            nvarchar(max) NULL,
    [status_id]        bigint NULL,
    [updated_at]       datetimeoffset NULL,
    [updated_by]       bigint NULL,
    CONSTRAINT [PK_cd_certificate] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_cd_certificate_certificate_no] UNIQUE ([certificate_no]),
    CONSTRAINT [FK_cd_certificate_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_cd_certificate_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_cd_certificate_cert_type_id] FOREIGN KEY ([cert_type_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_cd_certificate_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id])
);
GO

-- cd_renewal  (created in migration InitialCreate, 17 Oct)
-- columns added later: duration_years (allsp 19 Oct), invoiced_gov_fees (allsp 19 Oct), owner_invoice_id (allsp 19 Oct)
CREATE TABLE [dbo].[cd_renewal] (
    [id]                  bigint IDENTITY(1,1) NOT NULL,
    [certificate_id]      bigint NOT NULL,
    [created_at]          datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]          bigint NULL,
    [current_step]        smallint NULL,
    [duration_years]      smallint NULL,
    [inspector_dept]      nvarchar(150) NULL,
    [inspector_name]      nvarchar(150) NULL,
    [inspector_phone]     nvarchar(30) NULL,
    [invoiced_gov_fees]   decimal(14,2) NULL,
    [is_deleted]          bit NOT NULL,
    [new_certificate_id]  bigint NULL,
    [new_expiry_date]     date NULL,
    [owner_invoice_id]    bigint NULL,
    [remarks]             nvarchar(max) NULL,
    [renewal_date]        date NULL,
    [started_at]          date NULL,
    [status_id]           bigint NULL,
    [updated_at]          datetimeoffset NULL,
    [updated_by]          bigint NULL,
    CONSTRAINT [PK_cd_renewal] PRIMARY KEY ([id]),
    CONSTRAINT [FK_cd_renewal_certificate_id] FOREIGN KEY ([certificate_id]) REFERENCES [cd_certificate]([id]),
    CONSTRAINT [FK_cd_renewal_new_certificate_id] FOREIGN KEY ([new_certificate_id]) REFERENCES [cd_certificate]([id]),
    CONSTRAINT [FK_cd_renewal_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id])
);
GO


-- =====================================================
-- Fleet, staff & assets
-- =====================================================
-- vehicle  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[vehicle] (
    [id]                   bigint IDENTITY(1,1) NOT NULL,
    [capacity_litres]      decimal(14,3) NULL,
    [created_at]           datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]           bigint NULL,
    [default_driver_id]    bigint NULL,
    [is_active]            bit NOT NULL,
    [is_deleted]           bit NOT NULL,
    [plate_no]             nvarchar(20) NOT NULL,
    [registration_expiry]  date NULL,
    [updated_at]           datetimeoffset NULL,
    [updated_by]           bigint NULL,
    [vendor_id]            bigint NULL,
    CONSTRAINT [PK_vehicle] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_vehicle_plate_no] UNIQUE ([plate_no]),
    CONSTRAINT [FK_vehicle_default_driver_id] FOREIGN KEY ([default_driver_id]) REFERENCES [driver]([id]),
    CONSTRAINT [FK_vehicle_vendor_id] FOREIGN KEY ([vendor_id]) REFERENCES [vendor]([id])
);
GO

-- driver  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[driver] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [hazmat_permit_no]  nvarchar(50) NULL,
    [is_active]         bit NOT NULL,
    [is_deleted]        bit NOT NULL,
    [license_expiry]    date NULL,
    [license_no]        nvarchar(50) NULL,
    [name_ar]           nvarchar(150) NULL,
    [name_en]           nvarchar(150) NULL,
    [phone]             nvarchar(30) NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    [vendor_id]         bigint NULL,
    CONSTRAINT [PK_driver] PRIMARY KEY ([id]),
    CONSTRAINT [FK_driver_vendor_id] FOREIGN KEY ([vendor_id]) REFERENCES [vendor]([id])
);
GO

-- employee  (created in migration fullsp, 18 Oct)
CREATE TABLE [dbo].[employee] (
    [id]             bigint IDENTITY(1,1) NOT NULL,
    [branch_id]      bigint NULL,
    [created_at]     datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]     bigint NULL,
    [department_id]  bigint NULL,
    [email]          nvarchar(150) NULL,
    [emirates_id]    nvarchar(30) NULL,
    [employee_no]    nvarchar(30) NULL,
    [hire_date]      date NULL,
    [is_active]      bit NOT NULL,
    [is_deleted]     bit NOT NULL,
    [job_title]      nvarchar(100) NULL,
    [name_ar]        nvarchar(150) NULL,
    [name_en]        nvarchar(150) NULL,
    [notes]          nvarchar(500) NULL,
    [phone]          nvarchar(30) NULL,
    [updated_at]     datetimeoffset NULL,
    [updated_by]     bigint NULL,
    [user_id]        bigint NULL,
    CONSTRAINT [PK_employee] PRIMARY KEY ([id]),
    CONSTRAINT [FK_employee_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_employee_department_id] FOREIGN KEY ([department_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_employee_user_id] FOREIGN KEY ([user_id]) REFERENCES [app_user]([id])
);
GO

-- asset  (created in migration fullsp, 18 Oct)
CREATE TABLE [dbo].[asset] (
    [id]                    bigint IDENTITY(1,1) NOT NULL,
    [asset_code]            nvarchar(30) NULL,
    [assigned_employee_id]  bigint NULL,
    [branch_id]             bigint NULL,
    [building_id]           bigint NULL,
    [category_id]           bigint NULL,
    [created_at]            datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]            bigint NULL,
    [is_deleted]            bit NOT NULL,
    [name_ar]               nvarchar(150) NULL,
    [name_en]               nvarchar(150) NULL,
    [notes]                 nvarchar(500) NULL,
    [purchase_cost]         decimal(18,2) NULL,
    [purchase_date]         date NULL,
    [serial_no]             nvarchar(100) NULL,
    [status_id]             bigint NULL,
    [updated_at]            datetimeoffset NULL,
    [updated_by]            bigint NULL,
    [warranty_expiry]       date NULL,
    CONSTRAINT [PK_asset] PRIMARY KEY ([id]),
    CONSTRAINT [FK_asset_assigned_employee_id] FOREIGN KEY ([assigned_employee_id]) REFERENCES [employee]([id]),
    CONSTRAINT [FK_asset_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_asset_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_asset_category_id] FOREIGN KEY ([category_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_asset_status_id] FOREIGN KEY ([status_id]) REFERENCES [lookup_value]([id])
);
GO


-- =====================================================
-- Users, security & audit
-- =====================================================
-- app_user  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[app_user] (
    [id]             bigint IDENTITY(1,1) NOT NULL,
    [branch_id]      bigint NULL,
    [company_id]     bigint NOT NULL,
    [created_at]     datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]     bigint NULL,
    [email]          nvarchar(150) NOT NULL,
    [is_active]      bit NOT NULL,
    [is_deleted]     bit NOT NULL,
    [last_login_at]  datetimeoffset NULL,
    [name_ar]        nvarchar(150) NULL,
    [name_en]        nvarchar(150) NULL,
    [password_hash]  nvarchar(255) NULL,
    [phone]          nvarchar(30) NULL,
    [updated_at]     datetimeoffset NULL,
    [updated_by]     bigint NULL,
    CONSTRAINT [PK_app_user] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_app_user_email] UNIQUE ([email]),
    CONSTRAINT [FK_app_user_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_app_user_company_id] FOREIGN KEY ([company_id]) REFERENCES [company]([id])
);
GO

-- role  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[role] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [code]        nvarchar(50) NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [is_deleted]  bit NOT NULL,
    [name_ar]     nvarchar(100) NULL,
    [name_en]     nvarchar(100) NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    CONSTRAINT [PK_role] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_role_code] UNIQUE ([code])
);
GO

-- permission  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[permission] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [code]        nvarchar(100) NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [is_active]   bit NOT NULL,
    [is_deleted]  bit NOT NULL,
    [module]      nvarchar(50) NULL,
    [name_ar]     nvarchar(150) NULL,
    [name_en]     nvarchar(150) NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    CONSTRAINT [PK_permission] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_permission_code] UNIQUE ([code])
);
GO

-- role_permission  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[role_permission] (
    [role_id]        bigint NOT NULL,
    [permission_id]  bigint NOT NULL,
    [created_at]     datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]     bigint NULL,
    [is_deleted]     bit NOT NULL,
    [updated_at]     datetimeoffset NULL,
    [updated_by]     bigint NULL,
    CONSTRAINT [PK_role_permission] PRIMARY KEY ([role_id], [permission_id]),
    CONSTRAINT [FK_role_permission_role_id] FOREIGN KEY ([role_id]) REFERENCES [role]([id]),
    CONSTRAINT [FK_role_permission_permission_id] FOREIGN KEY ([permission_id]) REFERENCES [permission]([id])
);
GO

-- user_role  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[user_role] (
    [user_id]     bigint NOT NULL,
    [role_id]     bigint NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [is_deleted]  bit NOT NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    CONSTRAINT [PK_user_role] PRIMARY KEY ([user_id], [role_id]),
    CONSTRAINT [FK_user_role_user_id] FOREIGN KEY ([user_id]) REFERENCES [app_user]([id]),
    CONSTRAINT [FK_user_role_role_id] FOREIGN KEY ([role_id]) REFERENCES [role]([id])
);
GO

-- user_branch  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[user_branch] (
    [user_id]     bigint NOT NULL,
    [branch_id]   bigint NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [is_deleted]  bit NOT NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    CONSTRAINT [PK_user_branch] PRIMARY KEY ([user_id], [branch_id]),
    CONSTRAINT [FK_user_branch_user_id] FOREIGN KEY ([user_id]) REFERENCES [app_user]([id]),
    CONSTRAINT [FK_user_branch_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id])
);
GO

-- refresh_token  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[refresh_token] (
    [id]                   bigint IDENTITY(1,1) NOT NULL,
    [absolute_expires_at]  datetimeoffset NOT NULL,
    [created_at]           datetimeoffset NOT NULL,
    [created_ip]           nvarchar(64) NULL,
    [expires_at]           datetimeoffset NOT NULL,
    [family_id]            uniqueidentifier NOT NULL,
    [replaced_by_id]       bigint NULL,
    [revoked_at]           datetimeoffset NULL,
    [revoked_reason]       nvarchar(30) NULL,
    [session_started_at]   datetimeoffset NOT NULL,
    [token_hash]           nvarchar(100) NOT NULL,
    [user_agent]           nvarchar(300) NULL,
    [user_id]              bigint NOT NULL,
    CONSTRAINT [PK_refresh_token] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_refresh_token_token_hash_ux_refresh_token_hash] UNIQUE ([token_hash], [ux_refresh_token_hash]),
    CONSTRAINT [FK_refresh_token_user_id] FOREIGN KEY ([user_id]) REFERENCES [app_user]([id])
);
GO

-- audit_trail  (created in migration audit, 18 Oct)
CREATE TABLE [dbo].[audit_trail] (
    [id]              bigint IDENTITY(1,1) NOT NULL,
    [action]          nvarchar(20) NOT NULL,
    [branch_id]       bigint NULL,
    [changes]         nvarchar(max) NULL,
    [entity_id]       nvarchar(100) NULL,
    [entity_type]     nvarchar(100) NULL,
    [http_method]     nvarchar(10) NULL,
    [ip_address]      nvarchar(64) NULL,
    [occurred_at]     datetimeoffset NOT NULL,
    [request_path]    nvarchar(300) NULL,
    [table_name]      nvarchar(100) NULL,
    [transaction_id]  uniqueidentifier NOT NULL,
    [user_id]         bigint NULL,
    [user_name]       nvarchar(150) NULL,
    CONSTRAINT [PK_audit_trail] PRIMARY KEY ([id])
);
GO

-- audit_trail_link  (created in migration allsp, 19 Oct)
CREATE TABLE [dbo].[audit_trail_link] (
    [id]              bigint IDENTITY(1,1) NOT NULL,
    [audit_trail_id]  bigint NOT NULL,
    [entity_id]       nvarchar(100) NOT NULL,
    [entity_type]     nvarchar(100) NOT NULL,
    [is_self]         bit NOT NULL,
    CONSTRAINT [PK_audit_trail_link] PRIMARY KEY ([id]),
    CONSTRAINT [FK_audit_trail_link_audit_trail_id] FOREIGN KEY ([audit_trail_id]) REFERENCES [audit_trail]([id])
);
GO

-- activity_log  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[activity_log] (
    [id]           bigint IDENTITY(1,1) NOT NULL,
    [action]       nvarchar(50) NULL,
    [branch_id]    bigint NULL,
    [building_id]  bigint NULL,
    [created_at]   datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]   bigint NULL,
    [description]  nvarchar(max) NULL,
    [entity_id]    bigint NULL,
    [entity_type]  nvarchar(50) NULL,
    [is_deleted]   bit NOT NULL,
    [new_values]   nvarchar(max) NULL,
    [occurred_at]  datetimeoffset NOT NULL,
    [old_values]   nvarchar(max) NULL,
    [severity_id]  bigint NULL,
    [updated_at]   datetimeoffset NULL,
    [updated_by]   bigint NULL,
    [user_id]      bigint NULL,
    CONSTRAINT [PK_activity_log] PRIMARY KEY ([id]),
    CONSTRAINT [FK_activity_log_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_activity_log_building_id] FOREIGN KEY ([building_id]) REFERENCES [building]([id]),
    CONSTRAINT [FK_activity_log_severity_id] FOREIGN KEY ([severity_id]) REFERENCES [lookup_value]([id]),
    CONSTRAINT [FK_activity_log_user_id] FOREIGN KEY ([user_id]) REFERENCES [app_user]([id])
);
GO


-- =====================================================
-- Company setup & reference data
-- =====================================================
-- company  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[company] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [address]           nvarchar(max) NULL,
    [base_currency_id]  bigint NULL,
    [code]              nvarchar(20) NOT NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [email]             nvarchar(150) NULL,
    [is_active]         bit NOT NULL,
    [is_deleted]        bit NOT NULL,
    [logo_dark_path]    nvarchar(500) NULL,
    [logo_path]         nvarchar(500) NULL,
    [name_ar]           nvarchar(200) NOT NULL,
    [name_en]           nvarchar(200) NOT NULL,
    [phone]             nvarchar(30) NULL,
    [stamp_path]        nvarchar(500) NULL,
    [trn]               nvarchar(20) NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    [website]           nvarchar(150) NULL,
    CONSTRAINT [PK_company] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_company_code] UNIQUE ([code]),
    CONSTRAINT [FK_company_base_currency_id] FOREIGN KEY ([base_currency_id]) REFERENCES [currency]([id])
);
GO

-- branch  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[branch] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [address]     nvarchar(max) NULL,
    [city_id]     bigint NULL,
    [code]        nvarchar(20) NOT NULL,
    [company_id]  bigint NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [email]       nvarchar(150) NULL,
    [is_active]   bit NOT NULL,
    [is_deleted]  bit NOT NULL,
    [logo_path]   nvarchar(500) NULL,
    [name_ar]     nvarchar(200) NULL,
    [name_en]     nvarchar(200) NULL,
    [phone]       nvarchar(30) NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    CONSTRAINT [PK_branch] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_branch_code] UNIQUE ([code]),
    CONSTRAINT [FK_branch_city_id] FOREIGN KEY ([city_id]) REFERENCES [city]([id]),
    CONSTRAINT [FK_branch_company_id] FOREIGN KEY ([company_id]) REFERENCES [company]([id])
);
GO

-- country  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[country] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [code]        nvarchar(2) NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [is_active]   bit NOT NULL,
    [is_deleted]  bit NOT NULL,
    [name_ar]     nvarchar(100) NULL,
    [name_en]     nvarchar(100) NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    CONSTRAINT [PK_country] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_country_code] UNIQUE ([code])
);
GO

-- emirate  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[emirate] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [code]        nvarchar(10) NULL,
    [country_id]  bigint NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [is_deleted]  bit NOT NULL,
    [name_ar]     nvarchar(100) NULL,
    [name_en]     nvarchar(100) NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    CONSTRAINT [PK_emirate] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_emirate_code] UNIQUE ([code]),
    CONSTRAINT [FK_emirate_country_id] FOREIGN KEY ([country_id]) REFERENCES [country]([id])
);
GO

-- city  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[city] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [code]        nvarchar(20) NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [emirate_id]  bigint NOT NULL,
    [is_active]   bit NOT NULL,
    [is_deleted]  bit NOT NULL,
    [latitude]    decimal(9,6) NULL,
    [longitude]   decimal(9,6) NULL,
    [name_ar]     nvarchar(100) NULL,
    [name_en]     nvarchar(100) NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    CONSTRAINT [PK_city] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_city_code] UNIQUE ([code]),
    CONSTRAINT [FK_city_emirate_id] FOREIGN KEY ([emirate_id]) REFERENCES [emirate]([id])
);
GO

-- lookup_type  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[lookup_type] (
    [id]           bigint IDENTITY(1,1) NOT NULL,
    [code]         nvarchar(50) NOT NULL,
    [created_at]   datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]   bigint NULL,
    [description]  nvarchar(max) NULL,
    [is_active]    bit NOT NULL,
    [is_deleted]   bit NOT NULL,
    [name_ar]      nvarchar(100) NULL,
    [name_en]      nvarchar(100) NULL,
    [updated_at]   datetimeoffset NULL,
    [updated_by]   bigint NULL,
    CONSTRAINT [PK_lookup_type] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_lookup_type_code] UNIQUE ([code])
);
GO

-- lookup_value  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[lookup_value] (
    [id]              bigint IDENTITY(1,1) NOT NULL,
    [code]            nvarchar(50) NOT NULL,
    [created_at]      datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]      bigint NULL,
    [is_active]       bit NOT NULL,
    [is_deleted]      bit NOT NULL,
    [lookup_type_id]  bigint NOT NULL,
    [name_ar]         nvarchar(150) NULL,
    [name_en]         nvarchar(150) NULL,
    [parent_id]       bigint NULL,
    [sort_order]      int NULL,
    [updated_at]      datetimeoffset NULL,
    [updated_by]      bigint NULL,
    CONSTRAINT [PK_lookup_value] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_lookup_value_lookup_type_id_code] UNIQUE ([lookup_type_id], [code]),
    CONSTRAINT [FK_lookup_value_lookup_type_id] FOREIGN KEY ([lookup_type_id]) REFERENCES [lookup_type]([id]),
    CONSTRAINT [FK_lookup_value_parent_id] FOREIGN KEY ([parent_id]) REFERENCES [lookup_value]([id])
);
GO

-- number_sequence  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[number_sequence] (
    [id]          bigint IDENTITY(1,1) NOT NULL,
    [branch_id]   bigint NULL,
    [company_id]  bigint NOT NULL,
    [created_at]  datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]  bigint NULL,
    [is_deleted]  bit NOT NULL,
    [last_value]  int NULL,
    [padding]     smallint NULL,
    [prefix]      nvarchar(10) NOT NULL,
    [updated_at]  datetimeoffset NULL,
    [updated_by]  bigint NULL,
    [year]        smallint NULL,
    CONSTRAINT [PK_number_sequence] PRIMARY KEY ([id]),
    CONSTRAINT [UQ_number_sequence_company_id_branch_id_prefix_year] UNIQUE ([company_id], [branch_id], [prefix], [year]),
    CONSTRAINT [FK_number_sequence_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_number_sequence_company_id] FOREIGN KEY ([company_id]) REFERENCES [company]([id])
);
GO

-- document  (created in migration InitialCreate, 17 Oct)
CREATE TABLE [dbo].[document] (
    [id]                bigint IDENTITY(1,1) NOT NULL,
    [branch_id]         bigint NULL,
    [created_at]        datetimeoffset NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
    [created_by]        bigint NULL,
    [document_type_id]  bigint NULL,
    [entity_id]         bigint NOT NULL,
    [entity_type]       nvarchar(50) NOT NULL,
    [expiry_date]       date NULL,
    [file_name]         nvarchar(255) NULL,
    [file_path]         nvarchar(500) NULL,
    [is_deleted]        bit NOT NULL,
    [mime_type]         nvarchar(100) NULL,
    [size_bytes]        bigint NULL,
    [updated_at]        datetimeoffset NULL,
    [updated_by]        bigint NULL,
    CONSTRAINT [PK_document] PRIMARY KEY ([id]),
    CONSTRAINT [FK_document_branch_id] FOREIGN KEY ([branch_id]) REFERENCES [branch]([id]),
    CONSTRAINT [FK_document_document_type_id] FOREIGN KEY ([document_type_id]) REFERENCES [lookup_value]([id])
);
GO

