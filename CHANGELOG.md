
# **************************************************************************************
# Application Name : GXIntegration_Levis
# Author		   : Karen Ancheta
# Description      : Retail Pro Prism and S4 ERP Upgraded Integration Application.
#                    Prism Version : 1.14.6.1231
# 
# Notes            : 
# Use semantic versioning:
#    MAJOR → Breaking / incompatible change 
#    MINOR → New features, additive
#    PATCH → Small fixes or visual tweaks
#
# Version History  :
# -------------------------------------------------------------------------------
# Version | Part   | Date       | Developer       | Description
# -------------------------------------------------------------------------------
# 1.0.0   | MAJOR  | 2025-12-16 | Karen Ancheta   | Initial project creation.
# 2.0.0   | MAJOR  | 2026-01-28 | Karen Ancheta   | Changing UI framework from Guna1 to Guna2.
# 											        - Update all buttons design.
#                                                   - Update Navigation bar design.
#                                                   - Update Topbar design.
# 2.0.1   | PATCH  | 2026-03-05 | Karen Ancheta   | Update : Switch value for <RegularSalesUnitPrice> and <ActualSalesUnitPrice>
# 2.0.2   | PATCH  | 2026-03-23 | Karen Ancheta   | LEVIS Requested Adjustment For Gift Certificate (For StoreSale and StoreReturn).
# 											        - Change <Sale ItemType="dtv:NonMerchandise"> to <Sale ItemType="dtv:GiftCertificate">.
#                                                   - Remove the Tax Section for Gift Certificate transactions.
#                                                   - Bugfix on Sales <Percent> and <dtv:RawTaxPercentage> mapping.
# 2.0.3   | PATCH  | 2026-04-07 | Karen Ancheta   | Fix bug encountered on Auto processing.
#
#
#
#
# *************************************************************************************/
