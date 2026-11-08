-- Settings edited from the Admin tab, plus a log of every change.
create table DW_MDM_APP_SUP_SETTINGS
(
  setting_key    VARCHAR2(100) primary key,
  setting_value  VARCHAR2(500),
  updated_by     VARCHAR2(255),
  updated_on     DATE
)
tablespace DWH;

create table DW_MDM_APP_SUP_SETTINGS_LOG
(
  setting_key    VARCHAR2(100),
  setting_value  VARCHAR2(500),
  updated_by     VARCHAR2(255),
  updated_on     DATE
)
tablespace DWH;

-- Keys used by the portal:
--   CREATE_PROC       OWNER.PROCEDURE run at the EXECUTE step, called with (P_TEMPLATE_ID, P_REQUEST_ID)
--   CREATE_PROC_CONN  APPS or DWH
--   OU.ATS, OU.MLS, OU.TAR   operating unit written to SITE_OPERATING_UNIT for each company sheet
