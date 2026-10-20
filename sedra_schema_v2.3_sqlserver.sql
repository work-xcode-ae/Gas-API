-- =====================================================================
-- Sedra Petrol - Database Schema v2.3 - SQL Server (T-SQL)
-- 102 tables + audit columns + foreign keys + filtered unique indexes
-- Generated from sedra_schema_v2.3.dbml (the master copy). Do not edit by hand.
--
-- HOW TO USE (SSMS):
--   1. Run this whole script (F5) on an EMPTY database. It creates SedraPetrol_v23.
--      (Upgrading an existing v2.2 database needs a migration script, not this file.)
--   2. Object Explorer > SedraPetrol_v23 > Database Diagrams > New Database Diagram
--   3. Select all tables > Add > Close.  Right-click canvas > Arrange Tables.
-- =====================================================================
IF DB_ID(N'SedraPetrol_v23') IS NULL CREATE DATABASE [SedraPetrol_v23];
GO
USE [SedraPetrol_v23];
GO


CREATE TABLE [company] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [code] nvarchar(20) UNIQUE NOT NULL,
  [name_en] nvarchar(200) NOT NULL,
  [name_ar] nvarchar(200) NOT NULL,
  [address] nvarchar(max),
  [trn] nvarchar(20),
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
  [party_code] nvarchar(30),
  [party_type_id] bigint NOT NULL,
  [parent_party_id] bigint,
  [is_internal] bit DEFAULT (0),
  [name_en] nvarchar(200),
  [name_ar] nvarchar(200),
  [emirates_id] nvarchar(20),
  [nationality_id] bigint,
  [trade_license_no] nvarchar(50),
  [tax_number] nvarchar(30),
  [phone] nvarchar(30),
  [email] nvarchar(150),
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

CREATE TABLE [party_employee] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [party_id] bigint UNIQUE NOT NULL,
  [employee_no] nvarchar(30) UNIQUE NOT NULL,
  [employer_party_id] bigint,
  [branch_id] bigint,
  [department_id] bigint,
  [designation_id] bigint NOT NULL,
  [manager_party_id] bigint,
  [app_user_id] bigint,
  [date_of_birth] date,
  [join_date] date,
  [end_date] date,
  [employment_status_id] bigint,
  [mobile] nvarchar(30),
  [work_email] nvarchar(150),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [party_store] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [party_id] bigint UNIQUE NOT NULL,
  [branch_id] bigint NOT NULL,
  [building_id] bigint,
  [keeper_party_id] bigint,
  [allow_negative_stock] bit DEFAULT (0),
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
  [party_id] bigint UNIQUE NOT NULL,
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
  [payment_terms_days] smallint,
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

CREATE TABLE [vehicle] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [asset_id] bigint,
  [vendor_id] bigint,
  [vehicle_type_id] bigint,
  [default_driver_party_id] bigint,
  [plate_no] nvarchar(20) UNIQUE NOT NULL,
  [plate_source] nvarchar(30),
  [make] nvarchar(50),
  [model] nvarchar(50),
  [model_year] smallint,
  [chassis_no] nvarchar(50),
  [capacity_litres] decimal(14,3),
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
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0),
  PRIMARY KEY ([user_id], [role_id])
)
GO

CREATE TABLE [role_permission] (
  [role_id] bigint,
  [permission_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0),
  PRIMARY KEY ([role_id], [permission_id])
)
GO

CREATE TABLE [user_branch] (
  [user_id] bigint,
  [branch_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0),
  PRIMARY KEY ([user_id], [branch_id])
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
  [party_id] bigint UNIQUE NOT NULL,
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
  [asset_id] bigint,
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
  [asset_id] bigint,
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
  [driver_party_id] bigint,
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
  [driver_party_id] bigint,
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
  [customer_party_id] bigint NOT NULL,
  [tenant_id] bigint,
  [sales_order_id] bigint,
  [delivery_note_id] bigint,
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
  [item_id] bigint,
  [sales_order_line_id] bigint,
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
  [purchase_order_id] bigint,
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
  [item_id] bigint,
  [purchase_order_line_id] bigint,
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
  [party_id] bigint,
  [asset_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [document_type] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [code] nvarchar(50) UNIQUE NOT NULL,
  [name_en] nvarchar(150),
  [name_ar] nvarchar(150),
  [category_id] bigint,
  [applies_to] nvarchar(200),
  [has_number] bit DEFAULT (1),
  [has_expiry] bit DEFAULT (0),
  [allow_multiple] bit DEFAULT (0),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [reminder_offset] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [code] nvarchar(10) UNIQUE NOT NULL,
  [name_en] nvarchar(50),
  [name_ar] nvarchar(50),
  [offset_value] smallint NOT NULL,
  [offset_unit] nvarchar(10) NOT NULL,
  [sort_order] int DEFAULT (0),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [document_type_reminder] (
  [document_type_id] bigint,
  [reminder_offset_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0),
  PRIMARY KEY ([document_type_id], [reminder_offset_id])
)
GO

CREATE TABLE [document] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint,
  [document_type_id] bigint NOT NULL,
  [entity_type] nvarchar(50) NOT NULL,
  [entity_id] bigint NOT NULL,
  [party_id] bigint,
  [asset_id] bigint,
  [document_no] nvarchar(60),
  [title] nvarchar(200),
  [issue_date] date,
  [expiry_date] date,
  [issuing_authority] nvarchar(150),
  [status_id] bigint,
  [is_current] bit DEFAULT (1),
  [renewed_from_id] bigint,
  [remarks] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [document_file] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [document_id] bigint NOT NULL,
  [file_name] nvarchar(255) NOT NULL,
  [file_path] nvarchar(500) NOT NULL,
  [mime_type] nvarchar(100),
  [size_bytes] bigint,
  [page_label] nvarchar(30),
  [sort_order] int DEFAULT (0),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [document_reminder] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [document_id] bigint NOT NULL,
  [reminder_offset_id] bigint NOT NULL,
  [remind_on] date NOT NULL,
  [notify_user_id] bigint,
  [notify_role_id] bigint,
  [channel] nvarchar(20) DEFAULT 'in_app',
  [is_sent] bit DEFAULT (0),
  [sent_at] datetimeoffset(3),
  [is_dismissed] bit DEFAULT (0),
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

CREATE TABLE [asset] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [company_id] bigint NOT NULL,
  [branch_id] bigint NOT NULL,
  [asset_no] nvarchar(30) UNIQUE NOT NULL,
  [item_id] bigint,
  [asset_category_id] bigint NOT NULL,
  [name_en] nvarchar(200) NOT NULL,
  [name_ar] nvarchar(200),
  [serial_no] nvarchar(60),
  [brand] nvarchar(60),
  [model] nvarchar(60),
  [custodian_party_id] bigint NOT NULL,
  [building_id] bigint,
  [vendor_id] bigint,
  [purchase_order_id] bigint,
  [vendor_bill_id] bigint,
  [purchase_date] date,
  [purchase_cost] decimal(14,2),
  [currency_id] bigint,
  [warranty_expiry] date,
  [depreciation_method_id] bigint,
  [useful_life_months] smallint,
  [salvage_value] decimal(14,2),
  [accumulated_depreciation] decimal(14,2) DEFAULT (0),
  [asset_account_id] bigint,
  [status_id] bigint,
  [disposed_at] date,
  [disposal_notes] nvarchar(max),
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [asset_custody_history] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [asset_id] bigint NOT NULL,
  [from_party_id] bigint,
  [to_party_id] bigint NOT NULL,
  [from_date] datetimeoffset(3) NOT NULL,
  [to_date] datetimeoffset(3),
  [stock_transaction_line_id] bigint,
  [handed_by] bigint,
  [remarks] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [item] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [company_id] bigint NOT NULL,
  [item_code] nvarchar(30) UNIQUE NOT NULL,
  [barcode] nvarchar(60),
  [name_en] nvarchar(200) NOT NULL,
  [name_ar] nvarchar(200),
  [description] nvarchar(max),
  [item_type_id] bigint NOT NULL,
  [item_category_id] bigint,
  [uom_id] bigint NOT NULL,
  [brand] nvarchar(60),
  [is_serialized] bit DEFAULT (0),
  [track_stock] bit DEFAULT (1),
  [asset_category_id] bigint,
  [standard_cost] decimal(14,4),
  [sale_price] decimal(14,4),
  [vat_rate] decimal(5,2) DEFAULT (5),
  [reorder_level] decimal(14,3),
  [reorder_qty] decimal(14,3),
  [inventory_account_id] bigint,
  [expense_account_id] bigint,
  [revenue_account_id] bigint,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [item_stock] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [item_id] bigint NOT NULL,
  [location_party_id] bigint NOT NULL,
  [branch_id] bigint NOT NULL,
  [qty_on_hand] decimal(14,3) NOT NULL DEFAULT (0),
  [qty_reserved] decimal(14,3) NOT NULL DEFAULT (0),
  [avg_cost] decimal(14,4) DEFAULT (0),
  [last_txn_at] datetimeoffset(3),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [stock_transaction] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [txn_no] nvarchar(30) UNIQUE NOT NULL,
  [txn_type_id] bigint NOT NULL,
  [txn_date] datetimeoffset(3) NOT NULL,
  [from_party_id] bigint,
  [to_party_id] bigint,
  [from_branch_id] bigint,
  [to_branch_id] bigint,
  [reason_id] bigint,
  [source_type] nvarchar(40),
  [source_id] bigint,
  [status_id] bigint,
  [requested_by] bigint,
  [approved_by] bigint,
  [approved_at] datetimeoffset(3),
  [posted_by] bigint,
  [posted_at] datetimeoffset(3),
  [journal_entry_id] bigint,
  [remarks] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [stock_transaction_line] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [stock_transaction_id] bigint NOT NULL,
  [line_no] smallint,
  [item_id] bigint NOT NULL,
  [asset_id] bigint,
  [quantity] decimal(14,3) NOT NULL,
  [uom_id] bigint,
  [unit_cost] decimal(14,4),
  [amount] decimal(14,2),
  [material_request_line_id] bigint,
  [remarks] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [stock_ledger] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [item_id] bigint NOT NULL,
  [location_party_id] bigint NOT NULL,
  [branch_id] bigint NOT NULL,
  [stock_transaction_line_id] bigint NOT NULL,
  [txn_date] datetimeoffset(3) NOT NULL,
  [qty_in] decimal(14,3) DEFAULT (0),
  [qty_out] decimal(14,3) DEFAULT (0),
  [balance_qty] decimal(14,3) NOT NULL,
  [unit_cost] decimal(14,4),
  [value_in] decimal(14,2) DEFAULT (0),
  [value_out] decimal(14,2) DEFAULT (0),
  [balance_value] decimal(14,2),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [stock_count] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [count_no] nvarchar(30) UNIQUE NOT NULL,
  [location_party_id] bigint NOT NULL,
  [count_date] date NOT NULL,
  [counted_by_party_id] bigint,
  [status_id] bigint,
  [adjustment_txn_id] bigint,
  [remarks] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [stock_count_line] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [stock_count_id] bigint NOT NULL,
  [item_id] bigint NOT NULL,
  [asset_id] bigint,
  [system_qty] decimal(14,3) NOT NULL,
  [counted_qty] decimal(14,3),
  [difference_qty] decimal(14,3),
  [remarks] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [quotation] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [quotation_no] nvarchar(30) UNIQUE NOT NULL,
  [customer_party_id] bigint NOT NULL,
  [tenant_id] bigint,
  [building_id] bigint,
  [sales_person_party_id] bigint,
  [quotation_date] date NOT NULL,
  [valid_until] date,
  [currency_id] bigint,
  [exchange_rate] decimal(18,6) DEFAULT (1),
  [subtotal] decimal(14,2),
  [discount_amount] decimal(14,2) DEFAULT (0),
  [vat_amount] decimal(14,2) DEFAULT (0),
  [total_amount] decimal(14,2),
  [status_id] bigint,
  [terms] nvarchar(max),
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [quotation_line] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [quotation_id] bigint NOT NULL,
  [line_no] smallint,
  [item_id] bigint,
  [description] nvarchar(300),
  [quantity] decimal(14,3) NOT NULL,
  [uom_id] bigint,
  [unit_price] decimal(14,4),
  [discount_pct] decimal(5,2) DEFAULT (0),
  [vat_rate] decimal(5,2) DEFAULT (5),
  [amount] decimal(14,2),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [sales_order] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [so_no] nvarchar(30) UNIQUE NOT NULL,
  [quotation_id] bigint,
  [customer_party_id] bigint NOT NULL,
  [tenant_id] bigint,
  [building_id] bigint,
  [unit_id] bigint,
  [sales_person_party_id] bigint,
  [order_date] date NOT NULL,
  [expected_delivery_date] date,
  [currency_id] bigint,
  [exchange_rate] decimal(18,6) DEFAULT (1),
  [subtotal] decimal(14,2),
  [discount_amount] decimal(14,2) DEFAULT (0),
  [vat_amount] decimal(14,2) DEFAULT (0),
  [total_amount] decimal(14,2),
  [status_id] bigint,
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [sales_order_line] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [sales_order_id] bigint NOT NULL,
  [quotation_line_id] bigint,
  [line_no] smallint,
  [item_id] bigint,
  [description] nvarchar(300),
  [quantity] decimal(14,3) NOT NULL,
  [delivered_qty] decimal(14,3) DEFAULT (0),
  [invoiced_qty] decimal(14,3) DEFAULT (0),
  [uom_id] bigint,
  [unit_price] decimal(14,4),
  [discount_pct] decimal(5,2) DEFAULT (0),
  [vat_rate] decimal(5,2) DEFAULT (5),
  [amount] decimal(14,2),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [material_request] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [mr_no] nvarchar(30) UNIQUE NOT NULL,
  [request_date] date NOT NULL,
  [required_by_date] date,
  [requested_by_party_id] bigint NOT NULL,
  [department_id] bigint,
  [building_id] bigint,
  [service_request_id] bigint,
  [deliver_to_party_id] bigint,
  [priority_id] bigint,
  [status_id] bigint,
  [approved_by] bigint,
  [approved_at] datetimeoffset(3),
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [material_request_line] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [material_request_id] bigint NOT NULL,
  [line_no] smallint,
  [item_id] bigint NOT NULL,
  [description] nvarchar(300),
  [quantity] decimal(14,3) NOT NULL,
  [issued_qty] decimal(14,3) DEFAULT (0),
  [ordered_qty] decimal(14,3) DEFAULT (0),
  [uom_id] bigint,
  [remarks] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [purchase_order] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [po_no] nvarchar(30) UNIQUE NOT NULL,
  [vendor_id] bigint NOT NULL,
  [material_request_id] bigint,
  [po_date] date NOT NULL,
  [expected_date] date,
  [deliver_to_party_id] bigint,
  [currency_id] bigint,
  [exchange_rate] decimal(18,6) DEFAULT (1),
  [subtotal] decimal(14,2),
  [discount_amount] decimal(14,2) DEFAULT (0),
  [vat_amount] decimal(14,2) DEFAULT (0),
  [total_amount] decimal(14,2),
  [payment_terms] nvarchar(200),
  [status_id] bigint,
  [approved_by] bigint,
  [approved_at] datetimeoffset(3),
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [purchase_order_line] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [purchase_order_id] bigint NOT NULL,
  [material_request_line_id] bigint,
  [line_no] smallint,
  [item_id] bigint,
  [description] nvarchar(300),
  [quantity] decimal(14,3) NOT NULL,
  [received_qty] decimal(14,3) DEFAULT (0),
  [billed_qty] decimal(14,3) DEFAULT (0),
  [uom_id] bigint,
  [unit_price] decimal(14,4),
  [vat_rate] decimal(5,2) DEFAULT (5),
  [amount] decimal(14,2),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [delivery_note] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [dn_no] nvarchar(30) UNIQUE NOT NULL,
  [direction_id] bigint NOT NULL,
  [sales_order_id] bigint,
  [purchase_order_id] bigint,
  [customer_party_id] bigint,
  [vendor_id] bigint,
  [delivery_date] datetimeoffset(3) NOT NULL,
  [from_party_id] bigint,
  [to_party_id] bigint,
  [building_id] bigint,
  [unit_id] bigint,
  [vehicle_id] bigint,
  [driver_party_id] bigint,
  [received_by_name] nvarchar(150),
  [received_by_phone] nvarchar(30),
  [status_id] bigint,
  [stock_transaction_id] bigint,
  [notes] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [delivery_note_line] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [delivery_note_id] bigint NOT NULL,
  [line_no] smallint,
  [item_id] bigint NOT NULL,
  [asset_id] bigint,
  [sales_order_line_id] bigint,
  [purchase_order_line_id] bigint,
  [quantity] decimal(14,3) NOT NULL,
  [uom_id] bigint,
  [remarks] nvarchar(max),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [voucher] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [company_id] bigint NOT NULL,
  [branch_id] bigint NOT NULL,
  [voucher_no] nvarchar(30) UNIQUE NOT NULL,
  [voucher_type_id] bigint NOT NULL,
  [voucher_date] date NOT NULL,
  [party_id] bigint,
  [cash_bank_account_id] bigint,
  [payment_method_id] bigint,
  [cheque_no] nvarchar(30),
  [cheque_date] date,
  [bank_reference] nvarchar(100),
  [currency_id] bigint,
  [exchange_rate] decimal(18,6) DEFAULT (1),
  [total_amount] decimal(14,2) NOT NULL,
  [description] nvarchar(max),
  [source_type] nvarchar(40),
  [source_id] bigint,
  [status_id] bigint,
  [prepared_by] bigint,
  [approved_by] bigint,
  [approved_at] datetimeoffset(3),
  [posted_by] bigint,
  [posted_at] datetimeoffset(3),
  [journal_entry_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [voucher_line] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [voucher_id] bigint NOT NULL,
  [line_no] smallint,
  [account_id] bigint NOT NULL,
  [party_id] bigint,
  [description] nvarchar(300),
  [debit] decimal(14,2) DEFAULT (0),
  [credit] decimal(14,2) DEFAULT (0),
  [building_id] bigint,
  [unit_id] bigint,
  [tenant_id] bigint,
  [vendor_id] bigint,
  [contract_id] bigint,
  [asset_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [customer_request] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [branch_id] bigint NOT NULL,
  [request_no] nvarchar(30) UNIQUE NOT NULL,
  [request_type_id] bigint NOT NULL,
  [customer_party_id] bigint NOT NULL,
  [tenant_id] bigint,
  [contract_id] bigint,
  [building_id] bigint,
  [unit_id] bigint,
  [meter_id] bigint,
  [invoice_id] bigint,
  [payment_id] bigint,
  [channel_id] bigint,
  [reason_id] bigint,
  [request_date] datetimeoffset(3) NOT NULL,
  [description] nvarchar(max) NOT NULL,
  [requested_amount] decimal(14,2),
  [approved_amount] decimal(14,2),
  [priority_id] bigint,
  [status_id] bigint NOT NULL,
  [current_approval_step] smallint,
  [assigned_to_party_id] bigint,
  [voucher_id] bigint,
  [resolution] nvarchar(max),
  [closed_at] datetimeoffset(3),
  [closed_by] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [customer_request_operation] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [customer_request_id] bigint NOT NULL,
  [visit_date] datetimeoffset(3) NOT NULL,
  [employee_party_id] bigint NOT NULL,
  [meter_id] bigint,
  [meter_reading] decimal(14,3),
  [meter_reading_id] bigint,
  [issue_details] nvarchar(max),
  [remark] nvarchar(max),
  [next_action_id] bigint,
  [next_action_note] nvarchar(max),
  [next_action_date] date,
  [status_after_id] bigint,
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [approval_rule] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [company_id] bigint NOT NULL,
  [entity_type] nvarchar(50) NOT NULL,
  [request_type_id] bigint,
  [min_amount] decimal(14,2),
  [max_amount] decimal(14,2),
  [step_no] smallint NOT NULL,
  [approver_role_id] bigint NOT NULL,
  [is_active] bit DEFAULT (1),
  [created_at] datetimeoffset(3) NOT NULL DEFAULT (SYSDATETIMEOFFSET()),
  [created_by] bigint NULL,
  [updated_at] datetimeoffset(3) NULL,
  [updated_by] bigint NULL,
  [is_deleted] bit NOT NULL DEFAULT (0)
)
GO

CREATE TABLE [approval_step] (
  [id] bigint PRIMARY KEY IDENTITY(1, 1),
  [entity_type] nvarchar(50) NOT NULL,
  [entity_id] bigint NOT NULL,
  [step_no] smallint NOT NULL,
  [approver_role_id] bigint,
  [approver_user_id] bigint,
  [decision_id] bigint,
  [decided_by] bigint,
  [decided_at] datetimeoffset(3),
  [remarks] nvarchar(max),
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

CREATE INDEX [document_index_8] ON [document] ("party_id")
GO

CREATE INDEX [document_index_9] ON [document] ("asset_id")
GO

CREATE INDEX [document_index_10] ON [document] ("expiry_date")
GO

CREATE UNIQUE INDEX [document_reminder_index_11] ON [document_reminder] ("document_id", "reminder_offset_id")
GO

CREATE INDEX [document_reminder_index_12] ON [document_reminder] ("remind_on", "is_sent")
GO

CREATE UNIQUE INDEX [number_sequence_index_13] ON [number_sequence] ("company_id", "branch_id", "prefix", "year")
GO

CREATE UNIQUE INDEX [item_stock_index_14] ON [item_stock] ("item_id", "location_party_id")
GO

CREATE INDEX [stock_ledger_index_15] ON [stock_ledger] ("item_id", "location_party_id", "txn_date")
GO

CREATE UNIQUE INDEX [approval_step_index_16] ON [approval_step] ("entity_type", "entity_id", "step_no")
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'v2.3: logo, dark logo and stamp are documents (COMPANY_LOGO, COMPANY_LOGO_DARK, COMPANY_STAMP) on entity_type=company.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'company';
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
@value = 'BUILDING_TYPE, BUILDING_STATUS, UNIT_TYPE, UNIT_STATUS, METER_TYPE, METER_STATUS, METER_SERVICE_TYPE, READING_METHOD, CONTRACT_STATUS, FEE_TYPE, FEE_CYCLE, INVOICE_TYPE, INVOICE_STATUS, PAYMENT_METHOD, PAYMENT_STATUS, PARTY_TYPE, CONTACT_TYPE, VENDOR_TYPE, DOCUMENT_CATEGORY, DOCUMENT_STATUS, TANK_STATUS, REFILL_STAGE, REFILL_STATUS, PRIORITY, SR_STATUS, VISIT_TYPE, VISIT_STATUS, VISIT_FREQUENCY, SEVERITY, AMC_TERM_TYPE, SCOPE_ITEM_TYPE, CD_CERT_TYPE, CD_STATUS, ACCOUNT_TYPE, TANK_EVENT_TYPE, NATIONALITY, DEPARTMENT, DESIGNATION, EMPLOYMENT_STATUS, ASSET_CATEGORY, ASSET_STATUS, DEPRECIATION_METHOD, ITEM_TYPE, ITEM_CATEGORY, UOM, STOCK_TXN_TYPE, STOCK_TXN_STATUS, STOCK_REASON, QUOTATION_STATUS, SO_STATUS, PO_STATUS, DN_DIRECTION, DN_STATUS, MR_STATUS, VOUCHER_TYPE, VOUCHER_STATUS, CUSTOMER_REQUEST_TYPE, CUSTOMER_REQUEST_STATUS, REQUEST_CHANNEL, REQUEST_REASON, NEXT_ACTION, APPROVAL_DECISION',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'lookup_type',
@level2type = N'Column', @level2name = 'code';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'MASTER of every person / organisation / holding location. Extensions: party_employee (employee details), vendor (supplier details), tenant (customer details), party_store (stock / repository). Documents (EID, passport, licence...) live in document with party_id.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'P-000123 (number_sequence prefix P)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party',
@level2type = N'Column', @level2name = 'party_code';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'PARTY_TYPE: company | individual | employee | supplier | customer | stock | repository',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party',
@level2type = N'Column', @level2name = 'party_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'e.g. stock/repository -> owning company; supplier employee -> supplier',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party',
@level2type = N'Column', @level2name = 'parent_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'true = our own company, our employees, our stores',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party',
@level2type = N'Column', @level2name = 'is_internal';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'individual / employee - number only; the scan + expiry is a document (EID)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party',
@level2type = N'Column', @level2name = 'emirates_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'company / supplier - number only; scan + expiry is a document (TRADE_LICENSE)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party',
@level2type = N'Column', @level2name = 'trade_license_no';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Replaces the v2.2 driver table: a driver is an employee with designation DRIVER. Driving licence and hazmat permit are documents with expiry + reminders.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_employee';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'party_type = employee',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_employee',
@level2type = N'Column', @level2name = 'party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'null = our company; supplier party = supplier staff (e.g. supplier tanker driver)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_employee',
@level2type = N'Column', @level2name = 'employer_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'DEPARTMENT',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_employee',
@level2type = N'Column', @level2name = 'department_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'DESIGNATION: DRIVER | TECHNICIAN | ENGINEER | STOREKEEPER | ACCOUNTANT | SALES | SUPERVISOR | MANAGER | ...',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_employee',
@level2type = N'Column', @level2name = 'designation_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'set if the employee logs in to the portal',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_employee',
@level2type = N'Column', @level2name = 'app_user_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'active | on_leave | resigned | terminated',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_employee',
@level2type = N'Column', @level2name = 'employment_status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'party_type = stock | repository',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_store',
@level2type = N'Column', @level2name = 'party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'if the store sits in one of our buildings',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_store',
@level2type = N'Column', @level2name = 'building_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'employee responsible (storekeeper)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_store',
@level2type = N'Column', @level2name = 'keeper_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'phone | email',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'party_contact',
@level2type = N'Column', @level2name = 'contact_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'v2.3: name/licence/trn are kept for backward compatibility; party is the master. Trade licence & VAT certificate files are documents on the party.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vendor';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'supplier details extension of party (party_type = supplier)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vendor',
@level2type = N'Column', @level2name = 'party_id';
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
@name = N'Table_Description',
@value = 'Registration (Mulkiya), insurance and permits are documents on the asset (asset_id) with expiry + reminders.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vehicle';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'required for our own vehicles; null only for supplier tankers',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vehicle',
@level2type = N'Column', @level2name = 'asset_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'null = own company vehicle',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vehicle',
@level2type = N'Column', @level2name = 'vendor_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'ASSET_CATEGORY child: tanker | pickup | car | van',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vehicle',
@level2type = N'Column', @level2name = 'vehicle_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'employee with designation DRIVER',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vehicle',
@level2type = N'Column', @level2name = 'default_driver_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'Dubai, Abu Dhabi, Sharjah ...',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'vehicle',
@level2type = N'Column', @level2name = 'plate_source';
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
@name = N'Column_Description',
@value = 'customer details extension of party (party_type = customer / individual / company)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'tenant',
@level2type = N'Column', @level2name = 'party_id';
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
@value = 'company-owned meter as an asset',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'meter',
@level2type = N'Column', @level2name = 'asset_id';
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
@value = 'company-owned tank as an asset',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'tank',
@level2type = N'Column', @level2name = 'asset_id';
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
@value = 'employee, designation DRIVER',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'refill_collection',
@level2type = N'Column', @level2name = 'driver_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'employee, designation DRIVER',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'tank_refill',
@level2type = N'Column', @level2name = 'driver_party_id';
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
@value = 'v2.3: any customer party',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'invoice',
@level2type = N'Column', @level2name = 'customer_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'v2.3: optional - set for gas-supply invoices',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'invoice',
@level2type = N'Column', @level2name = 'tenant_id';
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
@value = 'amc_contract | refill_request | purchase_order | other',
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
@value = 'invoice | payment | vendor_bill | vendor_payment | voucher | stock_transaction | manual',
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
@value = 'analytic dimension (employee, customer, supplier)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'journal_line',
@level2type = N'Column', @level2name = 'party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'EID | PASSPORT | VISA | LABOUR_CARD | DRIVING_LICENSE | HAZMAT_PERMIT | TRADE_LICENSE | VAT_CERTIFICATE | VEHICLE_REGISTRATION | VEHICLE_INSURANCE | TITLE_DEED | CD_CERTIFICATE | CONTRACT_COPY | WARRANTY | COMPANY_LOGO | COMPANY_LOGO_DARK | COMPANY_STAMP | PHOTO | OTHER',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document_type',
@level2type = N'Column', @level2name = 'code';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'DOCUMENT_CATEGORY: identity | license | vehicle | contract | certificate | branding | photo | other',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document_type',
@level2type = N'Column', @level2name = 'category_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'comma list of entity types allowed, e.g. party,asset',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document_type',
@level2type = N'Column', @level2name = 'applies_to';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'false = one current document of this type per entity',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document_type',
@level2type = N'Column', @level2name = 'allow_multiple';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = '1Y | 1M | 1W (add 3M, 1D ... any time)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'reminder_offset',
@level2type = N'Column', @level2name = 'code';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = '1 year before',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'reminder_offset',
@level2type = N'Column', @level2name = 'name_en';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'day | week | month | year',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'reminder_offset',
@level2type = N'Column', @level2name = 'offset_unit';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'default reminders copied to document_reminder when a document of this type is created; user can change them per document',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document_type_reminder';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'ONE table for every attachment in the system. No other table stores file paths. Rule: entity_type=party => party_id = entity_id; entity_type=asset => asset_id = entity_id.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'party | asset | company | branch | building | unit | tenant | supply_contract | amc_contract | cd_certificate | meter_reading | maintenance_visit | customer_request | voucher | purchase_order | ...',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document',
@level2type = N'Column', @level2name = 'entity_type';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'set when entity_type = party (EID, passport, licence ...) - real FK to the party master',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document',
@level2type = N'Column', @level2name = 'party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'set when entity_type = asset (registration, insurance, warranty ...) - real FK to the asset master',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document',
@level2type = N'Column', @level2name = 'asset_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'e.g. EID 784-1990-1234567-1',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document',
@level2type = N'Column', @level2name = 'document_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'DOCUMENT_STATUS: valid | expiring | expired | renewed | cancelled (daily job)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'false once renewed',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document',
@level2type = N'Column', @level2name = 'is_current';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'previous version of the same document',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document',
@level2type = N'Column', @level2name = 'renewed_from_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'storage path / URL',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document_file',
@level2type = N'Column', @level2name = 'file_path';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'front | back | page 1 ...',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document_file',
@level2type = N'Column', @level2name = 'page_label';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'expiry_date - offset; recalculated when expiry_date changes',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document_reminder',
@level2type = N'Column', @level2name = 'remind_on';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'in_app | email | sms',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'document_reminder',
@level2type = N'Column', @level2name = 'channel';
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
@value = 'B | GSC | INV | RCP | RFR | SR | AMC | JE | PO | VP | P | EMP | AST | ITM | ST | SC | QT | SO | DN | GRN | MR | PV | RV | JV | DBN | CRN | CR',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'number_sequence',
@level2type = N'Column', @level2name = 'prefix';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'vehicle, tank and meter point here through asset_id. Registration / insurance / warranty files = document with asset_id.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'AST-YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset',
@level2type = N'Column', @level2name = 'asset_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'catalogue item this asset is a unit of (Item List <-> Assets)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset',
@level2type = N'Column', @level2name = 'item_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'ASSET_CATEGORY: vehicle | tank | meter | cylinder | equipment | tool | it | furniture | other',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset',
@level2type = N'Column', @level2name = 'asset_category_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'who holds it now: company | employee | stock | repository',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset',
@level2type = N'Column', @level2name = 'custodian_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'where it is installed, if in a building',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset',
@level2type = N'Column', @level2name = 'building_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'supplier',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset',
@level2type = N'Column', @level2name = 'vendor_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'straight_line | declining | none',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset',
@level2type = N'Column', @level2name = 'depreciation_method_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'cache - updated by depreciation run',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset',
@level2type = N'Column', @level2name = 'accumulated_depreciation';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'ASSET_STATUS: in_stock | in_use | under_maintenance | damaged | lost | disposed',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'null = current holder. Partial unique: one open row per asset',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset_custody_history',
@level2type = N'Column', @level2name = 'to_date';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'the movement that caused the hand-over',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'asset_custody_history',
@level2type = N'Column', @level2name = 'stock_transaction_line_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'ITM-NNNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'item',
@level2type = N'Column', @level2name = 'item_code';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'ITEM_TYPE: stock | asset | consumable | service',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'item',
@level2type = N'Column', @level2name = 'item_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'ITEM_CATEGORY: gas | meters | regulators | valves | pipes | cylinders | tools | spare_parts ...',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'item',
@level2type = N'Column', @level2name = 'item_category_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'UOM: each | m | litre | kg | m3 | box',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'item',
@level2type = N'Column', @level2name = 'uom_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'true = every unit is an asset row (meters, tanks, cylinders, vehicles)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'item',
@level2type = N'Column', @level2name = 'is_serialized';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'false for services',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'item',
@level2type = N'Column', @level2name = 'track_stock';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'default category for assets created from this item',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'item',
@level2type = N'Column', @level2name = 'asset_category_id';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'ITEM CURRENT QTY. Cache maintained only by posting stock_transaction; never edited by hand. Available = on_hand - reserved.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'item_stock';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'stock | repository | employee (van stock / custody)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'item_stock',
@level2type = N'Column', @level2name = 'location_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'confirmed sales orders / approved material requests',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'item_stock',
@level2type = N'Column', @level2name = 'qty_reserved';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'ITEM TRANSFER & ITEM TRANSACTIONS. Branch->branch, store->store, restock, employee->employee, damage ... all one table, told apart by txn_type.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_transaction';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'ST-YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_transaction',
@level2type = N'Column', @level2name = 'txn_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'STOCK_TXN_TYPE: opening | receipt | restock | issue | return | transfer_branch | transfer_location | transfer_employee | damage | write_off | adjustment | sale_delivery | consumption',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_transaction',
@level2type = N'Column', @level2name = 'txn_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'source store / employee (null for receipt / opening)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_transaction',
@level2type = N'Column', @level2name = 'from_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'target store / employee (null for damage / write_off / consumption)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_transaction',
@level2type = N'Column', @level2name = 'to_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'STOCK_REASON: broken | expired | lost | count_difference | customer_return ...',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_transaction',
@level2type = N'Column', @level2name = 'reason_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'material_request | purchase_order | delivery_note | stock_count | service_request | customer_request_operation | maintenance_visit',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_transaction',
@level2type = N'Column', @level2name = 'source_type';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'STOCK_TXN_STATUS: draft | pending_approval | approved | posted | cancelled',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_transaction',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'damage / write-off / adjustment / branch transfer valuation',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_transaction',
@level2type = N'Column', @level2name = 'journal_entry_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'serialized items: which unit moved (quantity = 1)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_transaction_line',
@level2type = N'Column', @level2name = 'asset_id';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'TRANSACTION HISTORY. Append-only. A transfer writes 2 rows (out of from_party, in to to_party). Corrections = a new reversing transaction.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_ledger';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'INVENTORY DETAILS: physical count of a store / employee custody.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_count';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'SC-YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_count',
@level2type = N'Column', @level2name = 'count_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'draft | counting | submitted | approved | posted',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_count',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'adjustment created from the differences',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'stock_count',
@level2type = N'Column', @level2name = 'adjustment_txn_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'QT-YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'quotation',
@level2type = N'Column', @level2name = 'quotation_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'employee',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'quotation',
@level2type = N'Column', @level2name = 'sales_person_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'QUOTATION_STATUS: draft | sent | accepted | rejected | expired | converted',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'quotation',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'SO-YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'sales_order',
@level2type = N'Column', @level2name = 'so_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'SO_STATUS: draft | confirmed | partially_delivered | delivered | invoiced | cancelled',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'sales_order',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Fulfilled from stock (stock_transaction issue/transfer) or converted to purchase_order.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'material_request';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'MR-YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'material_request',
@level2type = N'Column', @level2name = 'mr_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'employee',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'material_request',
@level2type = N'Column', @level2name = 'requested_by_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'store or employee to receive the items',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'material_request',
@level2type = N'Column', @level2name = 'deliver_to_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'MR_STATUS: draft | pending_approval | approved | rejected | partially_fulfilled | fulfilled | cancelled',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'material_request',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'General purchasing. refill_po stays for the gas refill workflow.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'purchase_order';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'PO-YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'purchase_order',
@level2type = N'Column', @level2name = 'po_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'receiving store',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'purchase_order',
@level2type = N'Column', @level2name = 'deliver_to_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'PO_STATUS: draft | pending_approval | approved | sent | partially_received | received | billed | closed | cancelled',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'purchase_order',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'DN-YYYY-NNNN (outbound) / GRN-YYYY-NNNN (inbound)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'delivery_note',
@level2type = N'Column', @level2name = 'dn_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'DN_DIRECTION: outbound (to customer) | inbound (goods received from supplier)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'delivery_note',
@level2type = N'Column', @level2name = 'direction_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'store (outbound)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'delivery_note',
@level2type = N'Column', @level2name = 'from_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'store (inbound)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'delivery_note',
@level2type = N'Column', @level2name = 'to_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'DN_STATUS: draft | dispatched | delivered | received | cancelled',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'delivery_note',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'stock movement posted from this note',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'delivery_note',
@level2type = N'Column', @level2name = 'stock_transaction_id';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Rule: SUM(debit) = SUM(credit) over voucher_line before approval.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'voucher';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'PV- | RV- | JV- | DBN- | CRN- YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'voucher',
@level2type = N'Column', @level2name = 'voucher_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'VOUCHER_TYPE: payment | receipt | journal | debit_note | credit_note | petty_cash',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'voucher',
@level2type = N'Column', @level2name = 'voucher_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'payee / payer',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'voucher',
@level2type = N'Column', @level2name = 'party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'payment / receipt vouchers',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'voucher',
@level2type = N'Column', @level2name = 'cash_bank_account_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'customer_request | vendor_bill | invoice | manual',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'voucher',
@level2type = N'Column', @level2name = 'source_type';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'VOUCHER_STATUS: draft | pending_approval | approved | posted | cancelled | reversed',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'voucher',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'created on posting (source_type = voucher)',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'voucher',
@level2type = N'Column', @level2name = 'journal_entry_id';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Status changes are logged in activity_log (action = status_change); approvals in approval_step.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'CR-YYYY-NNNN',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request',
@level2type = N'Column', @level2name = 'request_no';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'CUSTOMER_REQUEST_TYPE: refund | cancellation | deposit_refund | disconnection | reconnection | transfer_ownership | meter_change | billing_dispute | complaint | other',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request',
@level2type = N'Column', @level2name = 'request_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'REQUEST_CHANNEL: walk_in | phone | email | portal | whatsapp',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request',
@level2type = N'Column', @level2name = 'channel_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'REQUEST_REASON',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request',
@level2type = N'Column', @level2name = 'reason_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'CUSTOMER_REQUEST_STATUS: new | under_review | pending_approval | approved | rejected | in_progress | completed | cancelled | closed',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request',
@level2type = N'Column', @level2name = 'status_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'employee',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request',
@level2type = N'Column', @level2name = 'assigned_to_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'refund payment voucher',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request',
@level2type = N'Column', @level2name = 'voucher_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'technician / employee who did the visit',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request_operation',
@level2type = N'Column', @level2name = 'employee_party_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'reading taken on the visit',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request_operation',
@level2type = N'Column', @level2name = 'meter_reading';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'if saved as an official reading',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request_operation',
@level2type = N'Column', @level2name = 'meter_reading_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'NEXT_ACTION: revisit | disconnect | reconnect | replace_meter | refund | escalate | close',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request_operation',
@level2type = N'Column', @level2name = 'next_action_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'request status after this operation',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'customer_request_operation',
@level2type = N'Column', @level2name = 'status_after_id';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'customer_request | material_request | purchase_order | voucher | stock_transaction',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'approval_rule',
@level2type = N'Column', @level2name = 'entity_type';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'optional: rule only for this request / voucher / txn type',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'approval_rule',
@level2type = N'Column', @level2name = 'request_type_id';
GO

EXEC sp_addextendedproperty
@name = N'Table_Description',
@value = 'Generic approvals for customer requests, material requests, POs, vouchers and stock transactions. Steps are generated from approval_rule.',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'approval_step';
GO

EXEC sp_addextendedproperty
@name = N'Column_Description',
@value = 'APPROVAL_DECISION: pending | approved | rejected | returned',
@level0type = N'Schema', @level0name = 'dbo',
@level1type = N'Table',  @level1name = 'approval_step',
@level2type = N'Column', @level2name = 'decision_id';
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

ALTER TABLE [party] ADD FOREIGN KEY ([parent_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [party] ADD FOREIGN KEY ([nationality_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [party] ADD FOREIGN KEY ([city_id]) REFERENCES [city] ([id])
GO

ALTER TABLE [party_employee] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [party_employee] ADD FOREIGN KEY ([employer_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [party_employee] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [party_employee] ADD FOREIGN KEY ([department_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [party_employee] ADD FOREIGN KEY ([designation_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [party_employee] ADD FOREIGN KEY ([manager_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [party_employee] ADD FOREIGN KEY ([app_user_id]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [party_employee] ADD FOREIGN KEY ([employment_status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [party_store] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [party_store] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [party_store] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [party_store] ADD FOREIGN KEY ([keeper_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [party_contact] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [party_contact] ADD FOREIGN KEY ([contact_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [party_bank_account] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [vendor] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [vendor] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [vendor] ADD FOREIGN KEY ([vendor_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [vendor] ADD FOREIGN KEY ([payable_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [vendor_contact] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [vehicle] ADD FOREIGN KEY ([asset_id]) REFERENCES [asset] ([id])
GO

ALTER TABLE [vehicle] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [vehicle] ADD FOREIGN KEY ([vehicle_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [vehicle] ADD FOREIGN KEY ([default_driver_party_id]) REFERENCES [party] ([id])
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

ALTER TABLE [tenant] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
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

ALTER TABLE [meter] ADD FOREIGN KEY ([asset_id]) REFERENCES [asset] ([id])
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

ALTER TABLE [tank] ADD FOREIGN KEY ([asset_id]) REFERENCES [asset] ([id])
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

ALTER TABLE [refill_collection] ADD FOREIGN KEY ([driver_party_id]) REFERENCES [party] ([id])
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

ALTER TABLE [tank_refill] ADD FOREIGN KEY ([driver_party_id]) REFERENCES [party] ([id])
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

ALTER TABLE [invoice] ADD FOREIGN KEY ([customer_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [invoice] ADD FOREIGN KEY ([tenant_id]) REFERENCES [tenant] ([id])
GO

ALTER TABLE [invoice] ADD FOREIGN KEY ([sales_order_id]) REFERENCES [sales_order] ([id])
GO

ALTER TABLE [invoice] ADD FOREIGN KEY ([delivery_note_id]) REFERENCES [delivery_note] ([id])
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

ALTER TABLE [invoice_item] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [invoice_item] ADD FOREIGN KEY ([sales_order_line_id]) REFERENCES [sales_order_line] ([id])
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

ALTER TABLE [vendor_bill] ADD FOREIGN KEY ([purchase_order_id]) REFERENCES [purchase_order] ([id])
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

ALTER TABLE [vendor_bill_item] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [vendor_bill_item] ADD FOREIGN KEY ([purchase_order_line_id]) REFERENCES [purchase_order_line] ([id])
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

ALTER TABLE [journal_line] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [journal_line] ADD FOREIGN KEY ([asset_id]) REFERENCES [asset] ([id])
GO

ALTER TABLE [document_type] ADD FOREIGN KEY ([category_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [document_type_reminder] ADD FOREIGN KEY ([document_type_id]) REFERENCES [document_type] ([id])
GO

ALTER TABLE [document_type_reminder] ADD FOREIGN KEY ([reminder_offset_id]) REFERENCES [reminder_offset] ([id])
GO

ALTER TABLE [document] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [document] ADD FOREIGN KEY ([document_type_id]) REFERENCES [document_type] ([id])
GO

ALTER TABLE [document] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [document] ADD FOREIGN KEY ([asset_id]) REFERENCES [asset] ([id])
GO

ALTER TABLE [document] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [document] ADD FOREIGN KEY ([renewed_from_id]) REFERENCES [document] ([id])
GO

ALTER TABLE [document_file] ADD FOREIGN KEY ([document_id]) REFERENCES [document] ([id])
GO

ALTER TABLE [document_reminder] ADD FOREIGN KEY ([document_id]) REFERENCES [document] ([id])
GO

ALTER TABLE [document_reminder] ADD FOREIGN KEY ([reminder_offset_id]) REFERENCES [reminder_offset] ([id])
GO

ALTER TABLE [document_reminder] ADD FOREIGN KEY ([notify_user_id]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [document_reminder] ADD FOREIGN KEY ([notify_role_id]) REFERENCES [role] ([id])
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

ALTER TABLE [asset] ADD FOREIGN KEY ([company_id]) REFERENCES [company] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([asset_category_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([custodian_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([purchase_order_id]) REFERENCES [purchase_order] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([vendor_bill_id]) REFERENCES [vendor_bill] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([depreciation_method_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([asset_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [asset] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [asset_custody_history] ADD FOREIGN KEY ([asset_id]) REFERENCES [asset] ([id])
GO

ALTER TABLE [asset_custody_history] ADD FOREIGN KEY ([from_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [asset_custody_history] ADD FOREIGN KEY ([to_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [asset_custody_history] ADD FOREIGN KEY ([stock_transaction_line_id]) REFERENCES [stock_transaction_line] ([id])
GO

ALTER TABLE [asset_custody_history] ADD FOREIGN KEY ([handed_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [item] ADD FOREIGN KEY ([company_id]) REFERENCES [company] ([id])
GO

ALTER TABLE [item] ADD FOREIGN KEY ([item_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [item] ADD FOREIGN KEY ([item_category_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [item] ADD FOREIGN KEY ([uom_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [item] ADD FOREIGN KEY ([asset_category_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [item] ADD FOREIGN KEY ([inventory_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [item] ADD FOREIGN KEY ([expense_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [item] ADD FOREIGN KEY ([revenue_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [item_stock] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [item_stock] ADD FOREIGN KEY ([location_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [item_stock] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([txn_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([from_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([to_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([from_branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([to_branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([reason_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([requested_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([approved_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([posted_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [stock_transaction] ADD FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry] ([id])
GO

ALTER TABLE [stock_transaction_line] ADD FOREIGN KEY ([stock_transaction_id]) REFERENCES [stock_transaction] ([id])
GO

ALTER TABLE [stock_transaction_line] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [stock_transaction_line] ADD FOREIGN KEY ([asset_id]) REFERENCES [asset] ([id])
GO

ALTER TABLE [stock_transaction_line] ADD FOREIGN KEY ([uom_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [stock_transaction_line] ADD FOREIGN KEY ([material_request_line_id]) REFERENCES [material_request_line] ([id])
GO

ALTER TABLE [stock_ledger] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [stock_ledger] ADD FOREIGN KEY ([location_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [stock_ledger] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [stock_ledger] ADD FOREIGN KEY ([stock_transaction_line_id]) REFERENCES [stock_transaction_line] ([id])
GO

ALTER TABLE [stock_count] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [stock_count] ADD FOREIGN KEY ([location_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [stock_count] ADD FOREIGN KEY ([counted_by_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [stock_count] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [stock_count] ADD FOREIGN KEY ([adjustment_txn_id]) REFERENCES [stock_transaction] ([id])
GO

ALTER TABLE [stock_count_line] ADD FOREIGN KEY ([stock_count_id]) REFERENCES [stock_count] ([id])
GO

ALTER TABLE [stock_count_line] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [stock_count_line] ADD FOREIGN KEY ([asset_id]) REFERENCES [asset] ([id])
GO

ALTER TABLE [quotation] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [quotation] ADD FOREIGN KEY ([customer_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [quotation] ADD FOREIGN KEY ([tenant_id]) REFERENCES [tenant] ([id])
GO

ALTER TABLE [quotation] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [quotation] ADD FOREIGN KEY ([sales_person_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [quotation] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [quotation] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [quotation_line] ADD FOREIGN KEY ([quotation_id]) REFERENCES [quotation] ([id])
GO

ALTER TABLE [quotation_line] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [quotation_line] ADD FOREIGN KEY ([uom_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [sales_order] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [sales_order] ADD FOREIGN KEY ([quotation_id]) REFERENCES [quotation] ([id])
GO

ALTER TABLE [sales_order] ADD FOREIGN KEY ([customer_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [sales_order] ADD FOREIGN KEY ([tenant_id]) REFERENCES [tenant] ([id])
GO

ALTER TABLE [sales_order] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [sales_order] ADD FOREIGN KEY ([unit_id]) REFERENCES [unit] ([id])
GO

ALTER TABLE [sales_order] ADD FOREIGN KEY ([sales_person_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [sales_order] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [sales_order] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [sales_order_line] ADD FOREIGN KEY ([sales_order_id]) REFERENCES [sales_order] ([id])
GO

ALTER TABLE [sales_order_line] ADD FOREIGN KEY ([quotation_line_id]) REFERENCES [quotation_line] ([id])
GO

ALTER TABLE [sales_order_line] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [sales_order_line] ADD FOREIGN KEY ([uom_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [material_request] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [material_request] ADD FOREIGN KEY ([requested_by_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [material_request] ADD FOREIGN KEY ([department_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [material_request] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [material_request] ADD FOREIGN KEY ([service_request_id]) REFERENCES [service_request] ([id])
GO

ALTER TABLE [material_request] ADD FOREIGN KEY ([deliver_to_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [material_request] ADD FOREIGN KEY ([priority_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [material_request] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [material_request] ADD FOREIGN KEY ([approved_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [material_request_line] ADD FOREIGN KEY ([material_request_id]) REFERENCES [material_request] ([id])
GO

ALTER TABLE [material_request_line] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [material_request_line] ADD FOREIGN KEY ([uom_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [purchase_order] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [purchase_order] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [purchase_order] ADD FOREIGN KEY ([material_request_id]) REFERENCES [material_request] ([id])
GO

ALTER TABLE [purchase_order] ADD FOREIGN KEY ([deliver_to_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [purchase_order] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [purchase_order] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [purchase_order] ADD FOREIGN KEY ([approved_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [purchase_order_line] ADD FOREIGN KEY ([purchase_order_id]) REFERENCES [purchase_order] ([id])
GO

ALTER TABLE [purchase_order_line] ADD FOREIGN KEY ([material_request_line_id]) REFERENCES [material_request_line] ([id])
GO

ALTER TABLE [purchase_order_line] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [purchase_order_line] ADD FOREIGN KEY ([uom_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([direction_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([sales_order_id]) REFERENCES [sales_order] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([purchase_order_id]) REFERENCES [purchase_order] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([customer_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([from_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([to_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([unit_id]) REFERENCES [unit] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([vehicle_id]) REFERENCES [vehicle] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([driver_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [delivery_note] ADD FOREIGN KEY ([stock_transaction_id]) REFERENCES [stock_transaction] ([id])
GO

ALTER TABLE [delivery_note_line] ADD FOREIGN KEY ([delivery_note_id]) REFERENCES [delivery_note] ([id])
GO

ALTER TABLE [delivery_note_line] ADD FOREIGN KEY ([item_id]) REFERENCES [item] ([id])
GO

ALTER TABLE [delivery_note_line] ADD FOREIGN KEY ([asset_id]) REFERENCES [asset] ([id])
GO

ALTER TABLE [delivery_note_line] ADD FOREIGN KEY ([sales_order_line_id]) REFERENCES [sales_order_line] ([id])
GO

ALTER TABLE [delivery_note_line] ADD FOREIGN KEY ([purchase_order_line_id]) REFERENCES [purchase_order_line] ([id])
GO

ALTER TABLE [delivery_note_line] ADD FOREIGN KEY ([uom_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([company_id]) REFERENCES [company] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([voucher_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([cash_bank_account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([payment_method_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([currency_id]) REFERENCES [currency] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([prepared_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([approved_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([posted_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [voucher] ADD FOREIGN KEY ([journal_entry_id]) REFERENCES [journal_entry] ([id])
GO

ALTER TABLE [voucher_line] ADD FOREIGN KEY ([voucher_id]) REFERENCES [voucher] ([id])
GO

ALTER TABLE [voucher_line] ADD FOREIGN KEY ([account_id]) REFERENCES [account] ([id])
GO

ALTER TABLE [voucher_line] ADD FOREIGN KEY ([party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [voucher_line] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [voucher_line] ADD FOREIGN KEY ([unit_id]) REFERENCES [unit] ([id])
GO

ALTER TABLE [voucher_line] ADD FOREIGN KEY ([tenant_id]) REFERENCES [tenant] ([id])
GO

ALTER TABLE [voucher_line] ADD FOREIGN KEY ([vendor_id]) REFERENCES [vendor] ([id])
GO

ALTER TABLE [voucher_line] ADD FOREIGN KEY ([contract_id]) REFERENCES [supply_contract] ([id])
GO

ALTER TABLE [voucher_line] ADD FOREIGN KEY ([asset_id]) REFERENCES [asset] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([branch_id]) REFERENCES [branch] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([request_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([customer_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([tenant_id]) REFERENCES [tenant] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([contract_id]) REFERENCES [supply_contract] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([building_id]) REFERENCES [building] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([unit_id]) REFERENCES [unit] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([meter_id]) REFERENCES [meter] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([invoice_id]) REFERENCES [invoice] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([payment_id]) REFERENCES [payment] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([channel_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([reason_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([priority_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([status_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([assigned_to_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([voucher_id]) REFERENCES [voucher] ([id])
GO

ALTER TABLE [customer_request] ADD FOREIGN KEY ([closed_by]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [customer_request_operation] ADD FOREIGN KEY ([customer_request_id]) REFERENCES [customer_request] ([id])
GO

ALTER TABLE [customer_request_operation] ADD FOREIGN KEY ([employee_party_id]) REFERENCES [party] ([id])
GO

ALTER TABLE [customer_request_operation] ADD FOREIGN KEY ([meter_id]) REFERENCES [meter] ([id])
GO

ALTER TABLE [customer_request_operation] ADD FOREIGN KEY ([meter_reading_id]) REFERENCES [meter_reading] ([id])
GO

ALTER TABLE [customer_request_operation] ADD FOREIGN KEY ([next_action_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [customer_request_operation] ADD FOREIGN KEY ([status_after_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [approval_rule] ADD FOREIGN KEY ([company_id]) REFERENCES [company] ([id])
GO

ALTER TABLE [approval_rule] ADD FOREIGN KEY ([request_type_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [approval_rule] ADD FOREIGN KEY ([approver_role_id]) REFERENCES [role] ([id])
GO

ALTER TABLE [approval_step] ADD FOREIGN KEY ([approver_role_id]) REFERENCES [role] ([id])
GO

ALTER TABLE [approval_step] ADD FOREIGN KEY ([approver_user_id]) REFERENCES [app_user] ([id])
GO

ALTER TABLE [approval_step] ADD FOREIGN KEY ([decision_id]) REFERENCES [lookup_value] ([id])
GO

ALTER TABLE [approval_step] ADD FOREIGN KEY ([decided_by]) REFERENCES [app_user] ([id])
GO

-- ============ Filtered unique indexes (nullable unique columns) ============
CREATE UNIQUE INDEX [ux_emirate_code] ON [emirate] ([code]) WHERE [code] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_city_code] ON [city] ([code]) WHERE [code] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_party_party_code] ON [party] ([party_code]) WHERE [party_code] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_vendor_code] ON [vendor] ([code]) WHERE [code] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_vehicle_asset_id] ON [vehicle] ([asset_id]) WHERE [asset_id] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_tenant_emirates_id] ON [tenant] ([emirates_id]) WHERE [emirates_id] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_meter_asset_id] ON [meter] ([asset_id]) WHERE [asset_id] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_meter_service_service_no] ON [meter_service] ([service_no]) WHERE [service_no] IS NOT NULL;
GO
CREATE UNIQUE INDEX [ux_tank_asset_id] ON [tank] ([asset_id]) WHERE [asset_id] IS NOT NULL;
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
CREATE UNIQUE INDEX [ux_asset_custody_open] ON [asset_custody_history] ([asset_id]) WHERE [to_date] IS NULL;
GO
ALTER TABLE [journal_line] ADD CONSTRAINT [ck_journal_line_dr_cr] CHECK (([debit] = 0 AND [credit] > 0) OR ([credit] = 0 AND [debit] > 0));
GO
ALTER TABLE [voucher_line] ADD CONSTRAINT [ck_voucher_line_dr_cr] CHECK (([debit] = 0 AND [credit] > 0) OR ([credit] = 0 AND [debit] > 0));
GO
ALTER TABLE [payment_allocation] ADD CONSTRAINT [ck_payment_allocation_amount] CHECK ([amount] > 0);
GO
ALTER TABLE [vendor_payment_allocation] ADD CONSTRAINT [ck_vendor_payment_allocation_amount] CHECK ([amount] > 0);
GO
ALTER TABLE [stock_transaction_line] ADD CONSTRAINT [ck_stock_line_qty] CHECK ([quantity] > 0);
GO
ALTER TABLE [stock_transaction_line] ADD CONSTRAINT [ck_stock_line_asset_qty] CHECK ([asset_id] IS NULL OR [quantity] = 1);
GO
ALTER TABLE [stock_ledger] ADD CONSTRAINT [ck_stock_ledger_in_out] CHECK ([qty_in] >= 0 AND [qty_out] >= 0 AND ([qty_in] = 0 OR [qty_out] = 0));
GO
ALTER TABLE [document] ADD CONSTRAINT [ck_document_party_key] CHECK ([entity_type] <> 'party' OR [party_id] = [entity_id]);
GO
ALTER TABLE [document] ADD CONSTRAINT [ck_document_asset_key] CHECK ([entity_type] <> 'asset' OR [asset_id] = [entity_id]);
GO
ALTER TABLE [reminder_offset] ADD CONSTRAINT [ck_reminder_offset_unit] CHECK ([offset_unit] IN ('day','week','month','year') AND [offset_value] > 0);
GO

-- ============ Seed: reminder options ============
INSERT INTO [reminder_offset] ([code],[name_en],[name_ar],[offset_value],[offset_unit],[sort_order]) VALUES
 (N'1Y', N'1 year before',  N'قبل سنة',   1, 'year',  1),
 (N'1M', N'1 month before', N'قبل شهر',   1, 'month', 2),
 (N'1W', N'1 week before',  N'قبل أسبوع', 1, 'week',  3);
GO
