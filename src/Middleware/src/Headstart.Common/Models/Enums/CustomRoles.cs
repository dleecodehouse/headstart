namespace Headstart.Common.Models
{
    public enum CustomRole
    {
        // seller/supplier
        HSBuyerAdmin,
        HSBuyerImpersonator,
        HSCategoryAdmin,
        HSContentAdmin,
        HSMeAdmin,
        HSMeProductAdmin,
        HSMeSupplierAddressAdmin,
        HSMeSupplierAdmin,
        HSMeSupplierUserAdmin,
        HSOrderAdmin,
        HSOrderReader,
        HSProductAdmin,
        HSPromotionAdmin,
        HSReportAdmin,
        HSReportReader,
        HSSellerAdmin,
        HSShipmentAdmin,
        HSStorefrontAdmin,
        HSSupplierAdmin,
        HSSupplierUserGroupAdmin,
        HSOrderReturnApprover,

        // buyer
        HSBaseBuyer,
        HSLocationAddressAdmin,
        HSLocationOrderApprover,
        HSLocationViewAllOrders,
        HSLocationPermissionAdmin,
        HSLocationNeedsApproval,
        HSLocationCreditCardAdmin,

        // cms
        AssetAdmin,
        AssetReader,
        DocumentAdmin,
        DocumentReader,
        SchemaAdmin,
        SchemaReader,
    }
}
