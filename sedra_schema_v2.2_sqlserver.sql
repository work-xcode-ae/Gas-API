-- =====================================================================
-- Sedra Petrol - Database Schema v2.2 - SQL Server (T-SQL)
-- 71 tables + audit columns + foreign keys + filtered unique indexes
--
-- HOW TO USE (SSMS):
--   1. Run this whole script (F5). It creates the database SedraPetrol.
--   2. Object Explorer > SedraPetrol > Database Diagrams > New Database Diagram
--      (click "Yes" if asked to install diagram support)
--   3. Select all tables > Add > Close.  Right-click canvas > Arrange Tables.
-- =====================================================================
IF DB_ID(N'SedraPetrol') IS NULL CREATE DATABASE [SedraPetrol];
GO
USE [SedraPetrol];
GO

CREATE TABLE [company] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [code] nvarchar(20) UNIQUE NOT NULL,
  [name_en] nvarchar(200) NOT NULL,
  [name_ar] nvarchar(200) NOT NULL,
  [address] nvarchar(max),
  [trn] nvarchar(20),
  [logo_path] nvarchar(500),
  [logo_dark_path] nvarchar(500),
  [stamp_path] nvarchar(500),
  [phone] nvarchar(30),
  [email] nvarchar(150),
  [website] nvarchar(150),
  [base_currency_id] bigint,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [country] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [code] nchar(2) UNIQUE NOT NULL,
  [name_en] nvarchar(100),
  [name_ar] nvarchar(100),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [emirate] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [country_id] bigint NOT NULL,
  [code] nvarchar(10),
  [name_en] nvarchar(100),
  [name_ar] nvarchar(100),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [city] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [emirate_id] bigint NOT NULL,
  [code] nvarchar(20),
  [name_en] nvarchar(100),
  [name_ar] nvarchar(100),
  [latitude] decimal(9,6),
  [longitude] decimal(9,6),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [branch] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [company_id] bigint NOT NULL,
  [city_id] bigint,
  [code] nvarchar(20) UNIQUE NOT NULL,
  [name_en] nvarchar(200),
  [name_ar] nvarchar(200),
  [address] nvarchar(max),
  [phone] nvarchar(30),
  [email] nvarchar(150),
  [logo_path] nvarchar(500),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [lookup_type] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [code] nvarchar(50) UNIQUE NOT NULL,
  [name_en] nvarchar(100),
  [name_ar] nvarchar(100),
  [description] nvarchar(max),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [lookup_value] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [lookup_type_id] bigint NOT NULL,
  [code] nvarchar(50) NOT NULL,
  [name_en] nvarchar(150),
  [name_ar] nvarchar(150),
  [parent_id] bigint,
  [sort_order] int DEFAULT (0),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [party] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint,
  [party_type_id] bigint NOT NULL,
  [name_en] nvarchar(200),
  [name_ar] nvarchar(200),
  [emirates_id] nvarchar(20),
  [nationality_id] bigint,
  [trade_license_no] nvarchar(50),
  [tax_number] nvarchar(30),
  [city_id] bigint,
  [po_box] nvarchar(20),
  [address] nvarchar(max),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [party_contact] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [party_id] bigint NOT NULL,
  [contact_type_id] bigint NOT NULL,
  [value] nvarchar(150) NOT NULL,
  [is_default] bit DEFAULT (0),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [party_bank_account] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [party_id] bigint NOT NULL,
  [bank_name] nvarchar(150),
  [account_name] nvarchar(150),
  [iban] nvarchar(34),
  [is_default] bit DEFAULT (0),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [vendor] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint,
  [vendor_type_id] bigint NOT NULL,
  [code] nvarchar(30),
  [name_en] nvarchar(200),
  [name_ar] nvarchar(200),
  [trade_license_no] nvarchar(50),
  [trn] nvarchar(20),
  [phone] nvarchar(30),
  [email] nvarchar(150),
  [address] nvarchar(max),
  [payable_account_id] bigint,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [vendor_contact] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [vendor_id] bigint NOT NULL,
  [name_en] nvarchar(150),
  [name_ar] nvarchar(150),
  [position] nvarchar(100),
  [phone] nvarchar(30),
  [email] nvarchar(150),
  [is_default] bit DEFAULT (0),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [driver] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [vendor_id] bigint,
  [name_en] nvarchar(150),
  [name_ar] nvarchar(150),
  [license_no] nvarchar(50),
  [license_expiry] date,
  [hazmat_permit_no] nvarchar(50),
  [phone] nvarchar(30),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [vehicle] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [vendor_id] bigint,
  [default_driver_id] bigint,
  [plate_no] nvarchar(20) UNIQUE NOT NULL,
  [capacity_litres] decimal(14,3),
  [registration_expiry] date,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [app_user] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [company_id] bigint NOT NULL,
  [branch_id] bigint,
  [name_en] nvarchar(150),
  [name_ar] nvarchar(150),
  [email] nvarchar(150) UNIQUE NOT NULL,
  [phone] nvarchar(30),
  [password_hash] nvarchar(255),
  [last_login_at] datetimeoffset(3),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [role] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [code] nvarchar(50) UNIQUE NOT NULL,
  [name_en] nvarchar(100),
  [name_ar] nvarchar(100),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [permission] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [code] nvarchar(100) UNIQUE NOT NULL,
  [module] nvarchar(50),
  [name_en] nvarchar(150),
  [name_ar] nvarchar(150),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [user_role] (
  [user_id] bigint,
  [role_id] bigint,
  PRIMARY KEY ([user_id], [role_id]),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [role_permission] (
  [role_id] bigint,
  [permission_id] bigint,
  PRIMARY KEY ([role_id], [permission_id]),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [user_branch] (
  [user_id] bigint,
  [branch_id] bigint,
  PRIMARY KEY ([user_id], [branch_id]),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [building] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [code] nvarchar(20) UNIQUE NOT NULL,
  [name_en] nvarchar(200),
  [name_ar] nvarchar(200),
  [building_type_id] bigint,
  [status_id] bigint,
  [city_id] bigint,
  [zone] nvarchar(100),
  [street] nvarchar(150),
  [po_box] nvarchar(20),
  [address] nvarchar(max),
  [latitude] decimal(9,6),
  [longitude] decimal(9,6),
  [total_floors] int,
  [notes] nvarchar(max),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [building_owner] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [building_id] bigint NOT NULL,
  [party_id] bigint NOT NULL,
  [ownership_percentage] decimal(5,2),
  [is_primary] bit DEFAULT (0),
  [from_date] date,
  [to_date] date,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [unit] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [building_id] bigint NOT NULL,
  [unit_no] nvarchar(20) NOT NULL,
  [floor_no] int,
  [unit_type_id] bigint,
  [area_sqm] decimal(10,2),
  [bedrooms] smallint,
  [bathrooms] smallint,
  [rent_amount] decimal(14,2),
  [status_id] bigint,
  [last_inspection_date] date,
  [notes] nvarchar(max),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [tenant] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [tenant_code] nvarchar(20) UNIQUE NOT NULL,
  [name_en] nvarchar(200),
  [name_ar] nvarchar(200),
  [emirates_id] nvarchar(20),
  [nationality_id] bigint,
  [date_of_birth] date,
  [phone] nvarchar(30),
  [email] nvarchar(150),
  [city_id] bigint,
  [address] nvarchar(max),
  [receivable_account_id] bigint,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [supply_contract] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [contract_no] nvarchar(30) UNIQUE NOT NULL,
  [tenant_id] bigint NOT NULL,
  [unit_id] bigint NOT NULL,
  [meter_id] bigint,
  [start_date] date NOT NULL,
  [end_date] date,
  [auto_renew] bit DEFAULT (0),
  [renewal_period_months] smallint,
  [deposit_amount] decimal(14,2),
  [deposit_paid] bit DEFAULT (0),
  [deposit_date] date,
  [terms] nvarchar(max),
  [status_id] bigint,
  [terminated_at] date,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [contract_fee] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [contract_id] bigint NOT NULL,
  [fee_type_id] bigint NOT NULL,
  [cycle_id] bigint NOT NULL,
  [amount] decimal(14,4) NOT NULL,
  [currency_id] bigint,
  [effective_from] date NOT NULL,
  [effective_to] date,
  [notes] nvarchar(max),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [meter] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [meter_no] nvarchar(20) UNIQUE NOT NULL,
  [serial_no] nvarchar(50) UNIQUE NOT NULL,
  [meter_type_id] bigint,
  [brand] nvarchar(50),
  [model] nvarchar(50),
  [sim_card_no] nvarchar(30),
  [calibration_date] date,
  [install_date] date,
  [initial_reading] decimal(14,3) DEFAULT (0),
  [location_note] nvarchar(200),
  [status_id] bigint,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [meter_assignment] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [meter_id] bigint NOT NULL,
  [unit_id] bigint NOT NULL,
  [from_date] date NOT NULL,
  [to_date] date,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [meter_reading] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [meter_id] bigint NOT NULL,
  [reading_date] datetimeoffset(3) NOT NULL,
  [reading_value] decimal(14,3) NOT NULL,
  [consumption] decimal(14,3),
  [method_id] bigint,
  [read_by] bigint,
  [invoice_id] bigint,
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [meter_service] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [service_no] nvarchar(30),
  [meter_id] bigint NOT NULL,
  [contract_id] bigint,
  [service_type_id] bigint NOT NULL,
  [issue_type_id] bigint,
  [priority_id] bigint,
  [reason] nvarchar(max),
  [requested_date] date,
  [completed_date] date,
  [before_reading] decimal(14,3),
  [after_reading] decimal(14,3),
  [new_meter_id] bigint,
  [safety_check_passed] bit,
  [tenant_notified] bit,
  [technician_id] bigint,
  [fee_invoice_item_id] bigint,
  [status_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [meter_topup] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [meter_id] bigint NOT NULL,
  [topup_date] datetimeoffset(3) NOT NULL,
  [amount] decimal(14,2) NOT NULL,
  [quantity_m3] decimal(14,3),
  [payment_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [gas_type] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [code] nvarchar(20) UNIQUE NOT NULL,
  [name_en] nvarchar(100),
  [name_ar] nvarchar(100),
  [litre_to_m3] decimal(18,6) NOT NULL,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [tank] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [building_id] bigint NOT NULL,
  [tank_no] nvarchar(20) NOT NULL,
  [gas_type_id] bigint NOT NULL,
  [capacity_litres] decimal(14,3) NOT NULL,
  [current_level_litres] decimal(14,3),
  [serial_no] nvarchar(50),
  [manufacturer] nvarchar(100),
  [install_date] date,
  [last_inspection_date] date,
  [next_inspection_date] date,
  [status_id] bigint,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [tank_level_history] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [tank_id] bigint NOT NULL,
  [event_date] datetimeoffset(3) NOT NULL,
  [event_type_id] bigint,
  [level_litres] decimal(14,3) NOT NULL,
  [tank_refill_id] bigint,
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [refill_request] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [request_no] nvarchar(30) UNIQUE NOT NULL,
  [tank_id] bigint NOT NULL,
  [request_date] date NOT NULL,
  [requested_by] bigint,
  [requested_qty_litres] decimal(14,3) NOT NULL,
  [level_at_request_litres] decimal(14,3),
  [priority_id] bigint,
  [current_stage_id] bigint,
  [status_id] bigint,
  [approved_by] bigint,
  [approved_at] datetimeoffset(3),
  [approval_notes] nvarchar(max),
  [completed_at] datetimeoffset(3),
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [refill_stage_history] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [refill_request_id] bigint NOT NULL,
  [from_stage_id] bigint,
  [to_stage_id] bigint NOT NULL,
  [changed_by] bigint,
  [changed_at] datetimeoffset(3) NOT NULL,
  [remarks] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [refill_po] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [refill_request_id] bigint UNIQUE NOT NULL,
  [vendor_id] bigint NOT NULL,
  [po_number] nvarchar(30) UNIQUE NOT NULL,
  [po_date] date NOT NULL,
  [qty_litres] decimal(14,3),
  [unit_price] decimal(14,4),
  [total_amount] decimal(14,2),
  [currency_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [refill_supplier_payment] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [refill_request_id] bigint UNIQUE NOT NULL,
  [vendor_payment_id] bigint NOT NULL,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [refill_collection] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [refill_request_id] bigint UNIQUE NOT NULL,
  [vehicle_id] bigint,
  [driver_id] bigint,
  [collection_date] datetimeoffset(3),
  [meter_before] decimal(14,3),
  [meter_after] decimal(14,3),
  [collected_qty_litres] decimal(14,3),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [refill_purchase_invoice] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [refill_request_id] bigint UNIQUE NOT NULL,
  [vendor_bill_id] bigint NOT NULL,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [refill_schedule] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [refill_request_id] bigint UNIQUE NOT NULL,
  [delivery_date] date NOT NULL,
  [time_slot] nvarchar(30),
  [civil_defense_confirmed] bit DEFAULT (0),
  [civil_defense_ref] nvarchar(50),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [tank_refill] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [refill_request_id] bigint UNIQUE NOT NULL,
  [tank_id] bigint NOT NULL,
  [refill_datetime] datetimeoffset(3) NOT NULL,
  [vehicle_id] bigint,
  [driver_id] bigint,
  [level_before_litres] decimal(14,3),
  [level_after_litres] decimal(14,3),
  [delivered_qty_litres] decimal(14,3),
  [safety_check_passed] bit,
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [amc_contract] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [contract_no] nvarchar(30) UNIQUE NOT NULL,
  [vendor_id] bigint NOT NULL,
  [building_id] bigint NOT NULL,
  [owner_party_id] bigint,
  [start_date] date NOT NULL,
  [end_date] date NOT NULL,
  [annual_value] decimal(14,2),
  [currency_id] bigint,
  [visit_frequency_id] bigint,
  [visits_total] smallint,
  [response_time_hours] smallint,
  [emergency_included] bit DEFAULT (0),
  [status_id] bigint,
  [notes] nvarchar(max),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [amc_scope_item] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [contract_id] bigint NOT NULL,
  [item_type_id] bigint NOT NULL,
  [description] nvarchar(max) NOT NULL,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [amc_term] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [contract_id] bigint NOT NULL,
  [term_type_id] bigint,
  [description] nvarchar(max) NOT NULL,
  [is_custom] bit DEFAULT (0),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [amc_payment_schedule] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [contract_id] bigint NOT NULL,
  [label] nvarchar(100),
  [due_date] date NOT NULL,
  [amount] decimal(14,2) NOT NULL,
  [status_id] bigint,
  [vendor_bill_id] bigint,
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [checklist_template] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [visit_type_id] bigint,
  [name_en] nvarchar(150),
  [name_ar] nvarchar(150),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [checklist_template_item] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [template_id] bigint NOT NULL,
  [item_text_en] nvarchar(300),
  [item_text_ar] nvarchar(300),
  [sort_order] int,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [maintenance_visit] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [visit_no] nvarchar(30),
  [amc_contract_id] bigint NOT NULL,
  [building_id] bigint NOT NULL,
  [visit_type_id] bigint,
  [period_label] nvarchar(20),
  [scheduled_date] date NOT NULL,
  [time_slot] nvarchar(30),
  [visit_date] date,
  [vendor_contact_id] bigint,
  [technician_phone] nvarchar(30),
  [status_id] bigint,
  [report_summary] nvarchar(max),
  [completed_at] datetimeoffset(3),
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [visit_checklist_item] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [visit_id] bigint NOT NULL,
  [template_item_id] bigint,
  [item_text] nvarchar(300) NOT NULL,
  [sort_order] int,
  [is_done] bit DEFAULT (0),
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [visit_finding] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [visit_id] bigint NOT NULL,
  [severity_id] bigint,
  [description] nvarchar(max) NOT NULL,
  [action_taken] nvarchar(max),
  [service_request_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [service_request] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [request_no] nvarchar(30) UNIQUE NOT NULL,
  [building_id] bigint NOT NULL,
  [unit_id] bigint,
  [tenant_id] bigint,
  [meter_id] bigint,
  [description] nvarchar(max) NOT NULL,
  [priority_id] bigint,
  [status_id] bigint,
  [assigned_to] bigint,
  [assigned_vendor_id] bigint,
  [requested_at] datetimeoffset(3),
  [closed_at] datetimeoffset(3),
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [cd_certificate] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [building_id] bigint NOT NULL,
  [certificate_no] nvarchar(50),
  [cert_type_id] bigint,
  [license_no] nvarchar(50),
  [issued_by] nvarchar(150),
  [issue_date] date,
  [expiry_date] date,
  [scope] nvarchar(max),
  [inspector_name] nvarchar(150),
  [inspection_date] date,
  [status_id] bigint,
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [cd_renewal] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [certificate_id] bigint NOT NULL,
  [started_at] date,
  [current_step] smallint,
  [inspector_name] nvarchar(150),
  [inspector_phone] nvarchar(30),
  [inspector_dept] nvarchar(150),
  [renewal_date] date,
  [new_expiry_date] date,
  [new_certificate_id] bigint,
  [status_id] bigint,
  [remarks] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [invoice] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [invoice_no] nvarchar(30) UNIQUE NOT NULL,
  [tenant_id] bigint NOT NULL,
  [building_id] bigint,
  [contract_id] bigint,
  [invoice_type_id] bigint,
  [billing_period_start] date,
  [billing_period_end] date,
  [issue_date] date NOT NULL,
  [due_date] date NOT NULL,
  [subtotal] decimal(14,2),
  [vat_amount] decimal(14,2) DEFAULT (0),
  [total_amount] decimal(14,2),
  [currency_id] bigint,
  [exchange_rate] decimal(18,6) DEFAULT (1),
  [status_id] bigint,
  [journal_entry_id] bigint,
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [invoice_item] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [invoice_id] bigint NOT NULL,
  [item_type_id] bigint,
  [description] nvarchar(300),
  [quantity] decimal(14,3),
  [uom] nvarchar(10),
  [unit_price] decimal(14,4),
  [vat_rate] decimal(5,2) DEFAULT (5),
  [amount] decimal(14,2),
  [meter_reading_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [payment] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [receipt_no] nvarchar(30) UNIQUE NOT NULL,
  [tenant_id] bigint NOT NULL,
  [payment_date] date NOT NULL,
  [amount] decimal(14,2) NOT NULL,
  [currency_id] bigint,
  [payment_method_id] bigint,
  [reference_no] nvarchar(100),
  [received_by] bigint,
  [status_id] bigint,
  [journal_entry_id] bigint,
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [payment_allocation] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [payment_id] bigint NOT NULL,
  [invoice_id] bigint NOT NULL,
  [amount] decimal(14,2) NOT NULL,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [vendor_bill] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [vendor_id] bigint NOT NULL,
  [bill_no] nvarchar(50) NOT NULL,
  [bill_date] date NOT NULL,
  [due_date] date,
  [source_type] nvarchar(30),
  [source_id] bigint,
  [subtotal] decimal(14,2),
  [vat_amount] decimal(14,2) DEFAULT (0),
  [total_amount] decimal(14,2),
  [currency_id] bigint,
  [exchange_rate] decimal(18,6) DEFAULT (1),
  [status_id] bigint,
  [journal_entry_id] bigint,
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [vendor_bill_item] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [bill_id] bigint NOT NULL,
  [item_type_id] bigint,
  [description] nvarchar(300),
  [quantity] decimal(14,3),
  [uom] nvarchar(10),
  [unit_price] decimal(14,4),
  [vat_rate] decimal(5,2) DEFAULT (5),
  [amount] decimal(14,2),
  [expense_account_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [vendor_payment] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [payment_no] nvarchar(30) UNIQUE NOT NULL,
  [vendor_id] bigint NOT NULL,
  [payment_date] date NOT NULL,
  [amount] decimal(14,2) NOT NULL,
  [currency_id] bigint,
  [payment_method_id] bigint,
  [reference_no] nvarchar(100),
  [paid_by] bigint,
  [status_id] bigint,
  [journal_entry_id] bigint,
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [vendor_payment_allocation] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [vendor_payment_id] bigint NOT NULL,
  [vendor_bill_id] bigint NOT NULL,
  [amount] decimal(14,2) NOT NULL,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [currency] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [code] nchar(3) UNIQUE NOT NULL,
  [name_en] nvarchar(50),
  [name_ar] nvarchar(50),
  [symbol] nvarchar(10),
  [decimal_places] smallint DEFAULT (2),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [fiscal_period] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [company_id] bigint NOT NULL,
  [year] smallint NOT NULL,
  [period_no] smallint NOT NULL,
  [start_date] date NOT NULL,
  [end_date] date NOT NULL,
  [is_closed] bit DEFAULT (0),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [account] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [company_id] bigint NOT NULL,
  [parent_account_id] bigint,
  [account_type_id] bigint,
  [account_code] nvarchar(20) NOT NULL,
  [name_en] nvarchar(200),
  [name_ar] nvarchar(200),
  [level] smallint,
  [is_group] bit DEFAULT (0),
  [is_postable] bit DEFAULT (1),
  [currency_id] bigint,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [account_mapping] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [company_id] bigint NOT NULL,
  [branch_id] bigint,
  [source_type] nvarchar(50) NOT NULL,
  [source_key_id] bigint,
  [debit_account_id] bigint,
  [credit_account_id] bigint,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [journal_entry] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [company_id] bigint NOT NULL,
  [branch_id] bigint NOT NULL,
  [journal_no] nvarchar(30) UNIQUE NOT NULL,
  [entry_date] date NOT NULL,
  [fiscal_period_id] bigint NOT NULL,
  [currency_id] bigint,
  [exchange_rate] decimal(18,6) DEFAULT (1),
  [description] nvarchar(max),
  [source_type] nvarchar(50),
  [source_id] bigint,
  [status_id] bigint,
  [posted_at] datetimeoffset(3),
  [posted_by] bigint,
  [reversed_entry_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [journal_line] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [journal_entry_id] bigint NOT NULL,
  [account_id] bigint NOT NULL,
  [description] nvarchar(300),
  [debit] decimal(14,2) DEFAULT (0),
  [credit] decimal(14,2) DEFAULT (0),
  [base_debit] decimal(14,2) DEFAULT (0),
  [base_credit] decimal(14,2) DEFAULT (0),
  [building_id] bigint,
  [unit_id] bigint,
  [tenant_id] bigint,
  [vendor_id] bigint,
  [contract_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [document] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint,
  [entity_type] nvarchar(50) NOT NULL,
  [entity_id] bigint NOT NULL,
  [document_type_id] bigint,
  [file_name] nvarchar(255),
  [file_path] nvarchar(500),
  [mime_type] nvarchar(100),
  [size_bytes] bigint,
  [expiry_date] date,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [activity_log] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint,
  [building_id] bigint,
  [entity_type] nvarchar(50),
  [entity_id] bigint,
  [action] nvarchar(50),
  [severity_id] bigint,
  [description] nvarchar(max),
  [old_values] nvarchar(max),
  [new_values] nvarchar(max),
  [user_id] bigint,
  [occurred_at] datetimeoffset(3) NOT NULL,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [number_sequence] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [company_id] bigint NOT NULL,
  [branch_id] bigint,
  [prefix] nvarchar(10) NOT NULL,
  [year] smallint,
  [last_value] int DEFAULT (0),
  [padding] smallint DEFAULT (4),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE UNIQUE INDEX [lookup_value_index_0] ON [lookup_value] ("lookup_type_id", "code")
GO

CREATE UNIQUE INDEX [unit_index_1] ON [unit] ("building_id", "unit_no")
GO

CREATE INDEX [meter_reading_index_2] ON [meter_reading] ("meter_id", "reading_date")
GO

CREATE UNIQUE INDEX [tank_index_3] ON [tank] ("building_id", "tank_no")
GO

CREATE UNIQUE INDEX [vendor_bill_index_4] ON [vendor_bill] ("vendor_id", "bill_no")
GO

CREATE UNIQUE INDEX [fiscal_period_index_5] ON [fiscal_period] ("company_id", "year", "period_no")
GO

CREATE UNIQUE INDEX [account_index_6] ON [account] ("company_id", "account_code")
GO

CREATE INDEX [document_index_7] ON [document] ("entity_type", "entity_id")
GO

CREATE UNIQUE INDEX [number_sequence_index_8] ON [number_sequence] ("company_id", "branch_id", "prefix", "year")
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'UAE VAT Tax Registration Number',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'company',
@level2type = N'Column', @level2name = 'trn';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'main logo file (storage path/URL) - used on invoices, receipts, portal header',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'company',
@level2type = N'Column', @level2name = 'logo_path';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'optional: logo for dark backgrounds',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'company',
@level2type = N'Column', @level2name = 'logo_dark_path';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'optional: company stamp/seal for printed documents',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'company',
@level2type = N'Column', @level2name = 'stamp_path';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'optional: overrides company logo for this branch',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'branch',
@level2type = N'Column', @level2name = 'logo_path';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'BUILDING_TYPE, BUILDING_STATUS, UNIT_TYPE, UNIT_STATUS, METER_TYPE, METER_STATUS, METER_SERVICE_TYPE, READING_METHOD, CONTRACT_STATUS, FEE_TYPE, FEE_CYCLE, INVOICE_TYPE, INVOICE_STATUS, PAYMENT_METHOD, PAYMENT_STATUS, PARTY_TYPE, CONTACT_TYPE, VENDOR_TYPE, DOCUMENT_TYPE, TANK_STATUS, REFILL_STAGE, REFILL_STATUS, PRIORITY, SR_STATUS, VISIT_TYPE, VISIT_STATUS, VISIT_FREQUENCY, SEVERITY, AMC_TERM_TYPE, SCOPE_ITEM_TYPE, CD_CERT_TYPE, CD_STATUS, ACCOUNT_TYPE, TANK_EVENT_TYPE, NATIONALITY',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'lookup_type',
@level2type = N'Column', @level2name = 'code';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'individual | company',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party',
@level2type = N'Column', @level2name = 'party_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'individual',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party',
@level2type = N'Column', @level2name = 'emirates_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'company',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party',
@level2type = N'Column', @level2name = 'trade_license_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'phone | email',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_contact',
@level2type = N'Column', @level2name = 'contact_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'GAS_SUPPLIER | MAINTENANCE_VENDOR | SERVICE_VENDOR | OTHER',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vendor',
@level2type = N'Column', @level2name = 'vendor_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'e.g. Engineer / Technician',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vendor_contact',
@level2type = N'Column', @level2name = 'position';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'null = own company driver',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'driver',
@level2type = N'Column', @level2name = 'vendor_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'null = own company tanker',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vehicle',
@level2type = N'Column', @level2name = 'vendor_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'home branch',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'app_user',
@level2type = N'Column', @level2name = 'branch_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'e.g. buildings.create, invoices.post',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'permission',
@level2type = N'Column', @level2name = 'code';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'B-001',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'building',
@level2type = N'Column', @level2name = 'code';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'residential | commercial | mixed',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'building',
@level2type = N'Column', @level2name = 'building_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'active | inactive',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'building',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'apartment | penthouse | studio | duplex | villa | commercial',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'unit',
@level2type = N'Column', @level2name = 'unit_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'informational only',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'unit',
@level2type = N'Column', @level2name = 'rent_amount';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'vacant | occupied | reserved | maintenance',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'unit',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Partial unique index: one ACTIVE contract per unit.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'supply_contract';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'single numbering scheme: GSC-YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'supply_contract',
@level2type = N'Column', @level2name = 'contract_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'draft | active | pending | expired | terminated',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'supply_contract',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'gas_price | maintenance | connection | disconnection | inspection | meter_rent | insurance | other',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'contract_fee',
@level2type = N'Column', @level2name = 'fee_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'monthly | yearly | one_time | per_m3',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'contract_fee',
@level2type = N'Column', @level2name = 'cycle_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'smart | standard | prepaid',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'meter',
@level2type = N'Column', @level2name = 'meter_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'active | fault | disconnected | maintenance',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'meter',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'null = current. Partial unique: one open row per meter and per unit',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'meter_assignment',
@level2type = N'Column', @level2name = 'to_date';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'cumulative m3',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'meter_reading',
@level2type = N'Column', @level2name = 'reading_value';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'value - previous value',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'meter_reading',
@level2type = N'Column', @level2name = 'consumption';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'manual | remote | customer | photo',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'meter_reading',
@level2type = N'Column', @level2name = 'method_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'set when billed; prevents double billing',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'meter_reading',
@level2type = N'Column', @level2name = 'invoice_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'connect | disconnect | replace | report_issue',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'meter_service',
@level2type = N'Column', @level2name = 'service_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'for replace',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'meter_service',
@level2type = N'Column', @level2name = 'new_meter_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'LPG | NG',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'gas_type',
@level2type = N'Column', @level2name = 'code';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'conversion factor for billing',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'gas_type',
@level2type = N'Column', @level2name = 'litre_to_m3';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'cache of latest tank_level_history',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'tank',
@level2type = N'Column', @level2name = 'current_level_litres';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'ok | low | maintenance | out_of_service',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'tank',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'refill | manual_reading | sensor | adjustment',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'tank_level_history',
@level2type = N'Column', @level2name = 'event_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'RFR-YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'refill_request',
@level2type = N'Column', @level2name = 'request_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'REFILL_STAGE 0..8',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'refill_request',
@level2type = N'Column', @level2name = 'current_stage_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'requested | po_issued | paid | collecting | invoiced | scheduled | delivering | pending_approval | completed | cancelled',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'refill_request',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'stage 7',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'refill_request',
@level2type = N'Column', @level2name = 'approved_by';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'signing owner',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'amc_contract',
@level2type = N'Column', @level2name = 'owner_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'monthly | quarterly | semi_annual | annual',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'amc_contract',
@level2type = N'Column', @level2name = 'visit_frequency_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'pending | active | expired',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'amc_contract',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'included | excluded',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'amc_scope_item',
@level2type = N'Column', @level2name = 'item_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'category',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'amc_term',
@level2type = N'Column', @level2name = 'term_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'pending | billed | paid',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'amc_payment_schedule',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Q4 2026',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'maintenance_visit',
@level2type = N'Column', @level2name = 'period_label';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'technician',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'maintenance_visit',
@level2type = N'Column', @level2name = 'vendor_contact_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'scheduled | in_progress | completed | cancelled',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'maintenance_visit',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'copied from template, editable',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'visit_checklist_item',
@level2type = N'Column', @level2name = 'item_text';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'SR-YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'service_request',
@level2type = N'Column', @level2name = 'request_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'low | medium | high',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'service_request',
@level2type = N'Column', @level2name = 'priority_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'pending | in_progress | completed',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'service_request',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'valid | expired | renewal_in_progress',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'cd_certificate',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'gas_consumption | maintenance | deposit | connection | other',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'invoice',
@level2type = N'Column', @level2name = 'invoice_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'draft | pending | partial | paid | overdue | cancelled',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'invoice',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'FEE_TYPE',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'invoice_item',
@level2type = N'Column', @level2name = 'item_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'm3 | month | each',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'invoice_item',
@level2type = N'Column', @level2name = 'uom';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'cash | bank_transfer | cheque | online | card',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'payment',
@level2type = N'Column', @level2name = 'payment_method_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'vendor invoice number',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vendor_bill',
@level2type = N'Column', @level2name = 'bill_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'amc_contract | refill_request | other',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vendor_bill',
@level2type = N'Column', @level2name = 'source_type';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'AED',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'currency',
@level2type = N'Column', @level2name = 'code';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'asset | liability | equity | revenue | expense',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'account',
@level2type = N'Column', @level2name = 'account_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'null = all branches',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'account_mapping',
@level2type = N'Column', @level2name = 'branch_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'invoice_item | payment | vendor_bill_item | vendor_payment | deposit | vat',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'account_mapping',
@level2type = N'Column', @level2name = 'source_type';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'fee type / payment method / etc.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'account_mapping',
@level2type = N'Column', @level2name = 'source_key_id';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Rule: SUM(debit) = SUM(credit) per entry before posting. No posting into closed fiscal_period.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'journal_entry';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'invoice | payment | vendor_bill | vendor_payment | manual',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'journal_entry',
@level2type = N'Column', @level2name = 'source_type';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'draft | posted | reversed',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'journal_entry',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'in company base currency',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'journal_line',
@level2type = N'Column', @level2name = 'base_debit';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'analytic dimension',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'journal_line',
@level2type = N'Column', @level2name = 'building_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'building | party | tenant | supply_contract | amc_contract | meter_reading | maintenance_visit | cd_certificate | ...',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document',
@level2type = N'Column', @level2name = 'entity_type';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'create | update | delete | status_change | post',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'activity_log',
@level2type = N'Column', @level2name = 'action';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'B | GSC | INV | RCP | RFR | SR | AMC | JE | PO | VP',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'number_sequence',
@level2type = N'Column', @level2name = 'prefix';
GO

ALTER TABLE [company] ADD FOREIGN KEY ([base_currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [emirate] ADD FOREIGN KEY ([country_id]) REFERENCES [country] ([id])
GO

ALTER TABLE [city] ADD FOREIGN KEY ([emirate_id]) REFERENCES [emirate] ([id])
GO

ALTER TABLE [branch] ADD FOREIGN KEY ([company_id]) REFERENCES [company] ([id])
GO

ALTER TABLE [branch] ADD FOREIGN KEY ([city_id]) REFERENCES [city] ([id])
GO

ALTER TABLE [lookup_value] ADD FOREIGN KEY ([lookup_type_id]) REFERENCES [lookup_type] ([id])
GO

ALTER TABLE [lookup_value] ADD FOREIGN KEY ([parent_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [party] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [party] ADD FOREIGN KEY ([party_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [party] ADD FOREIGN KEY ([nationality_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [party] ADD FOREIGN KEY ([city_id]) REFERENCES [city] ([id])
GO

ALTER TABLE [party_contact] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [party_contact] ADD FOREIGN KEY ([contact_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [party_bank_account] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [vendor] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [vendor] ADD FOREIGN KEY ([vendor_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [vendor] ADD FOREIGN KEY ([payable_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [vendor_contact] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [driver] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [vehicle] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [vehicle] ADD FOREIGN KEY ([default_driver_id]) REFERENCES [driver] ([id])
GO

ALTER TABLE [app_user] ADD FOREIGN KEY ([company_id]) REFERENCES [company] ([id])
GO

ALTER TABLE [app_user] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [user_role] ADD FOREIGN KEY ([user_id]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [user_role] ADD FOREIGN KEY ([role_id]) REFERENCES [role] ([id])
GO

ALTER TABLE [role_permission] ADD FOREIGN KEY ([role_id]) REFERENCES [role] ([id])
GO

ALTER TABLE [role_permission] ADD FOREIGN KEY ([permission_id]) REFERENCES [permission] ([id])
GO

ALTER TABLE [user_branch] ADD FOREIGN KEY ([user_id]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [user_branch] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [building] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [building] ADD FOREIGN KEY ([building_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [building] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [building] ADD FOREIGN KEY ([city_id]) REFERENCES [city] ([id])
GO

ALTER TABLE [building_owner] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [building_owner] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [unit] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [unit] ADD FOREIGN KEY ([unit_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [unit] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [tenant] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [tenant] ADD FOREIGN KEY ([nationality_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [tenant] ADD FOREIGN KEY ([city_id]) REFERENCES [city] ([id])
GO

ALTER TABLE [tenant] ADD FOREIGN KEY ([receivable_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [supply_contract] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [supply_contract] ADD FOREIGN KEY ([tenant_id]) REFERENCES [tenant] ([id])
GO

ALTER TABLE [supply_contract] ADD FOREIGN KEY ([unit_id]) REFERENCES [unit] ([id])
GO

ALTER TABLE [supply_contract] ADD FOREIGN KEY ([meter_id]) REFERENCES [meter] ([id])
GO

ALTER TABLE [supply_contract] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [contract_fee] ADD FOREIGN KEY ([contract_id]) REFERENCES [supply_contract] ([id])
GO

ALTER TABLE [contract_fee] ADD FOREIGN KEY ([fee_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [contract_fee] ADD FOREIGN KEY ([cycle_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [contract_fee] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [meter] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [meter] ADD FOREIGN KEY ([meter_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [meter] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [meter_assignment] ADD FOREIGN KEY ([meter_id]) REFERENCES [meter] ([id])
GO

ALTER TABLE [meter_assignment] ADD FOREIGN KEY ([unit_id]) REFERENCES [unit] ([id])
GO

ALTER TABLE [meter_reading] ADD FOREIGN KEY ([meter_id]) REFERENCES [meter] ([id])
GO

ALTER TABLE [meter_reading] ADD FOREIGN KEY ([method_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [meter_reading] ADD FOREIGN KEY ([read_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [meter_reading] ADD FOREIGN KEY ([invoice_id]) REFERENCES [invoice] ([id])
GO

ALTER TABLE [meter_service] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [meter_service] ADD FOREIGN KEY ([meter_id]) REFERENCES [meter] ([id])
GO

ALTER TABLE [meter_service] ADD FOREIGN KEY ([contract_id]) REFERENCES [supply_contract] ([id])
GO

ALTER TABLE [meter_service] ADD FOREIGN KEY ([service_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [meter_service] ADD FOREIGN KEY ([issue_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [meter_service] ADD FOREIGN KEY ([priority_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [meter_service] ADD FOREIGN KEY ([new_meter_id]) REFERENCES [meter] ([id])
GO

ALTER TABLE [meter_service] ADD FOREIGN KEY ([technician_id]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [meter_service] ADD FOREIGN KEY ([fee_invoice_item_id]) REFERENCES [invoice_item] ([id])
GO

ALTER TABLE [meter_service] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [meter_topup] ADD FOREIGN KEY ([meter_id]) REFERENCES [meter] ([id])
GO

ALTER TABLE [meter_topup] ADD FOREIGN KEY ([payment_id]) REFERENCES [payment] ([id])
GO

ALTER TABLE [tank] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [tank] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [tank] ADD FOREIGN KEY ([gas_type_id]) REFERENCES [gas_type] ([id])
GO

ALTER TABLE [tank] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [tank_level_history] ADD FOREIGN KEY ([tank_id]) REFERENCES [tank] ([id])
GO

ALTER TABLE [tank_level_history] ADD FOREIGN KEY ([event_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [tank_level_history] ADD FOREIGN KEY ([tank_refill_id]) REFERENCES [tank_refill] ([id])
GO

ALTER TABLE [refill_request] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [refill_request] ADD FOREIGN KEY ([tank_id]) REFERENCES [tank] ([id])
GO

ALTER TABLE [refill_request] ADD FOREIGN KEY ([requested_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [refill_request] ADD FOREIGN KEY ([priority_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [refill_request] ADD FOREIGN KEY ([current_stage_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [refill_request] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [refill_request] ADD FOREIGN KEY ([approved_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [refill_stage_history] ADD FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request] ([id])
GO

ALTER TABLE [refill_stage_history] ADD FOREIGN KEY ([from_stage_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [refill_stage_history] ADD FOREIGN KEY ([to_stage_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [refill_stage_history] ADD FOREIGN KEY ([changed_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [refill_po] ADD FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request] ([id])
GO

ALTER TABLE [refill_po] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [refill_po] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [refill_supplier_payment] ADD FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request] ([id])
GO

ALTER TABLE [refill_supplier_payment] ADD FOREIGN KEY ([vendor_payment_id]) REFERENCES [vendor_payment] ([id])
GO

ALTER TABLE [refill_collection] ADD FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request] ([id])
GO

ALTER TABLE [refill_collection] ADD FOREIGN KEY ([vehicle_id]) REFERENCES [vehicle] ([id])
GO

ALTER TABLE [refill_collection] ADD FOREIGN KEY ([driver_id]) REFERENCES [driver] ([id])
GO

ALTER TABLE [refill_purchase_invoice] ADD FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request] ([id])
GO

ALTER TABLE [refill_purchase_invoice] ADD FOREIGN KEY ([vendor_bill_id]) REFERENCES [vendor_bill] ([id])
GO

ALTER TABLE [refill_schedule] ADD FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request] ([id])
GO

ALTER TABLE [tank_refill] ADD FOREIGN KEY ([refill_request_id]) REFERENCES [refill_request] ([id])
GO

ALTER TABLE [tank_refill] ADD FOREIGN KEY ([tank_id]) REFERENCES [tank] ([id])
GO

ALTER TABLE [tank_refill] ADD FOREIGN KEY ([vehicle_id]) REFERENCES [vehicle] ([id])
GO

ALTER TABLE [tank_refill] ADD FOREIGN KEY ([driver_id]) REFERENCES [driver] ([id])
GO

ALTER TABLE [amc_contract] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [amc_contract] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [amc_contract] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [amc_contract] ADD FOREIGN KEY ([owner_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [amc_contract] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [amc_contract] ADD FOREIGN KEY ([visit_frequency_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [amc_contract] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [amc_scope_item] ADD FOREIGN KEY ([contract_id]) REFERENCES [amc_contract] ([id])
GO

ALTER TABLE [amc_scope_item] ADD FOREIGN KEY ([item_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [amc_term] ADD FOREIGN KEY ([contract_id]) REFERENCES [amc_contract] ([id])
GO

ALTER TABLE [amc_term] ADD FOREIGN KEY ([term_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [amc_payment_schedule] ADD FOREIGN KEY ([contract_id]) REFERENCES [amc_contract] ([id])
GO

ALTER TABLE [amc_payment_schedule] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [amc_payment_schedule] ADD FOREIGN KEY ([vendor_bill_id]) REFERENCES [vendor_bill] ([id])
GO

ALTER TABLE [checklist_template] ADD FOREIGN KEY ([visit_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [checklist_template_item] ADD FOREIGN KEY ([template_id]) REFERENCES [checklist_template] ([id])
GO

ALTER TABLE [maintenance_visit] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [maintenance_visit] ADD FOREIGN KEY ([amc_contract_id]) REFERENCES [amc_contract] ([id])
GO

ALTER TABLE [maintenance_visit] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [maintenance_visit] ADD FOREIGN KEY ([visit_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [maintenance_visit] ADD FOREIGN KEY ([vendor_contact_id]) REFERENCES [vendor_contact] ([id])
GO

ALTER TABLE [maintenance_visit] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [visit_checklist_item] ADD FOREIGN KEY ([visit_id]) REFERENCES [maintenance_visit] ([id])
GO

ALTER TABLE [visit_checklist_item] ADD FOREIGN KEY ([template_item_id]) REFERENCES [checklist_template_item] ([id])
GO

ALTER TABLE [visit_finding] ADD FOREIGN KEY ([visit_id]) REFERENCES [maintenance_visit] ([id])
GO

ALTER TABLE [visit_finding] ADD FOREIGN KEY ([severity_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [visit_finding] ADD FOREIGN KEY ([service_request_id]) REFERENCES [service_request] ([id])
GO

ALTER TABLE [service_request] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [service_request] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [service_request] ADD FOREIGN KEY ([unit_id]) REFERENCES [unit] ([id])
GO

ALTER TABLE [service_request] ADD FOREIGN KEY ([tenant_id]) REFERENCES [tenant] ([id])
GO

ALTER TABLE [service_request] ADD FOREIGN KEY ([meter_id]) REFERENCES [meter] ([id])
GO

ALTER TABLE [service_request] ADD FOREIGN KEY ([priority_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [service_request] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [service_request] ADD FOREIGN KEY ([assigned_to]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [service_request] ADD FOREIGN KEY ([assigned_vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [cd_certificate] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [cd_certificate] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [cd_certificate] ADD FOREIGN KEY ([cert_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [cd_certificate] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [cd_renewal] ADD FOREIGN KEY ([certificate_id]) REFERENCES [cd_certificate] ([id])
GO

ALTER TABLE [cd_renewal] ADD FOREIGN KEY ([new_certificate_id]) REFERENCES [cd_certificate] ([id])
GO

ALTER TABLE [cd_renewal] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [invoice] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [invoice] ADD FOREIGN KEY ([tenant_id]) REFERENCES [tenant] ([id])
GO

ALTER TABLE [invoice] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [invoice] ADD FOREIGN KEY ([contract_id]) REFERENCES [supply_contract] ([id])
GO

ALTER TABLE [invoice] ADD FOREIGN KEY ([invoice_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [invoice] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [invoice] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [invoice] ADD FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry] ([id])
GO

ALTER TABLE [invoice_item] ADD FOREIGN KEY ([invoice_id]) REFERENCES [invoice] ([id])
GO

ALTER TABLE [invoice_item] ADD FOREIGN KEY ([item_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [invoice_item] ADD FOREIGN KEY ([meter_reading_id]) REFERENCES [meter_reading] ([id])
GO

ALTER TABLE [payment] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [payment] ADD FOREIGN KEY ([tenant_id]) REFERENCES [tenant] ([id])
GO

ALTER TABLE [payment] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [payment] ADD FOREIGN KEY ([payment_method_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [payment] ADD FOREIGN KEY ([received_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [payment] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [payment] ADD FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry] ([id])
GO

ALTER TABLE [payment_allocation] ADD FOREIGN KEY ([payment_id]) REFERENCES [payment] ([id])
GO

ALTER TABLE [payment_allocation] ADD FOREIGN KEY ([invoice_id]) REFERENCES [invoice] ([id])
GO

ALTER TABLE [vendor_bill] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [vendor_bill] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [vendor_bill] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [vendor_bill] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [vendor_bill] ADD FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry] ([id])
GO

ALTER TABLE [vendor_bill_item] ADD FOREIGN KEY ([bill_id]) REFERENCES [vendor_bill] ([id])
GO

ALTER TABLE [vendor_bill_item] ADD FOREIGN KEY ([item_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [vendor_bill_item] ADD FOREIGN KEY ([expense_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [vendor_payment] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [vendor_payment] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [vendor_payment] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [vendor_payment] ADD FOREIGN KEY ([payment_method_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [vendor_payment] ADD FOREIGN KEY ([paid_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [vendor_payment] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [vendor_payment] ADD FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry] ([id])
GO

ALTER TABLE [vendor_payment_allocation] ADD FOREIGN KEY ([vendor_payment_id]) REFERENCES [vendor_payment] ([id])
GO

ALTER TABLE [vendor_payment_allocation] ADD FOREIGN KEY ([vendor_bill_id]) REFERENCES [vendor_bill] ([id])
GO

ALTER TABLE [fiscal_period] ADD FOREIGN KEY ([company_id]) REFERENCES [company] ([id])
GO

ALTER TABLE [account] ADD FOREIGN KEY ([company_id]) REFERENCES [company] ([id])
GO

ALTER TABLE [account] ADD FOREIGN KEY ([parent_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [account] ADD FOREIGN KEY ([account_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [account] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [account_mapping] ADD FOREIGN KEY ([company_id]) REFERENCES [company] ([id])
GO

ALTER TABLE [account_mapping] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [account_mapping] ADD FOREIGN KEY ([source_key_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [account_mapping] ADD FOREIGN KEY ([debit_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [account_mapping] ADD FOREIGN KEY ([credit_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [journal_entry] ADD FOREIGN KEY ([company_id]) REFERENCES [company] ([id])
GO

ALTER TABLE [journal_entry] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [journal_entry] ADD FOREIGN KEY ([fiscal_period_id]) REFERENCES [fiscal_period] ([id])
GO

ALTER TABLE [journal_entry] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [journal_entry] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [journal_entry] ADD FOREIGN KEY ([posted_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [journal_entry] ADD FOREIGN KEY ([reversed_entry_id]) REFERENCES [journal_entry] ([id])
GO

ALTER TABLE [journal_line] ADD FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry] ([id])
GO

ALTER TABLE [journal_line] ADD FOREIGN KEY ([account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [journal_line] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [journal_line] ADD FOREIGN KEY ([unit_id]) REFERENCES [unit] ([id])
GO

ALTER TABLE [journal_line] ADD FOREIGN KEY ([tenant_id]) REFERENCES [tenant] ([id])
GO

ALTER TABLE [journal_line] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [journal_line] ADD FOREIGN KEY ([contract_id]) REFERENCES [supply_contract] ([id])
GO

ALTER TABLE [document] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [document] ADD FOREIGN KEY ([document_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [activity_log] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [activity_log] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [activity_log] ADD FOREIGN KEY ([severity_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [activity_log] ADD FOREIGN KEY ([user_id]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [number_sequence] ADD FOREIGN KEY ([company_id]) REFERENCES [company] ([id])
GO

ALTER TABLE [number_sequence] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

-- ============ Filtered unique indexes (nullable unique columns) ============
CREATE UNIQUE INDEX [ux_emirate_code] ON [emirate] ([code]) WHERE [code] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_city_code] ON [city] ([code]) WHERE [code] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_vendor_code] ON [vendor] ([code]) WHERE [code] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_tenant_emirates_id] ON [tenant] ([emirates_id]) WHERE [emirates_id] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_meter_service_service_no] ON [meter_service] ([service_no]) WHERE [service_no] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_maintenance_visit_visit_no] ON [maintenance_visit] ([visit_no]) WHERE [visit_no] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_cd_certificate_certificate_no] ON [cd_certificate] ([certificate_no]) WHERE [certificate_no] IS NOT NULL;
GO

-- ============ Business rules ============
CREATE UNIQUE INDEX [ux_meter_assignment_open_meter] ON [meter_assignment] ([meter_id]) WHERE [to_date] IS NULL;
GO
CREATE UNIQUE INDEX [ux_meter_assignment_open_unit] ON [meter_assignment] ([unit_id]) WHERE [to_date] IS NULL;
GO
CREATE UNIQUE INDEX [ux_supply_contract_active_unit] ON [supply_contract] ([unit_id]) WHERE [is_active] = 1 AND [is_deleted] = 0;
GO
CREATE UNIQUE INDEX [ux_building_owner_primary] ON [building_owner] ([building_id]) WHERE [is_primary] = 1;
GO
ALTER TABLE [journal_line] ADD CONSTRAINT [ck_journal_line_dr_cr] CHECK (([debit] = 0 AND [credit] > 0) OR ([credit] = 0 AND [debit] > 0));
GO
ALTER TABLE [payment_allocation] ADD CONSTRAINT [ck_payment_allocation_amount] CHECK ([amount] > 0);
GO
ALTER TABLE [vendor_payment_allocation] ADD CONSTRAINT [ck_vendor_payment_allocation_amount] CHECK ([amount] > 0);
GO
