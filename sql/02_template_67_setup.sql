-- The portal reuses the MDM Portal's request and approval tables for template 67
-- (New Supplier Creation). Check these exist before go-live:
--
--   DW_MDM_APP_TEMPLATES        row for TEMPLATE_ID = 67, AREA = 'Suppliers'
--   DW_MDM_APP_TEMPLATES_ROLES  who may raise template 67 requests (USER_ROLE or USER_NAME)
--   approval chain for 67       whatever DW_PROC_MDM_APPROVALS reads to build the steps,
--                               ending in an EXECUTE step for the MDM team
--
-- Quick check:
select * from DW_MDM_APP_TEMPLATES where template_id = 67;
select * from DW_MDM_APP_TEMPLATES_ROLES where template_id = 67;
