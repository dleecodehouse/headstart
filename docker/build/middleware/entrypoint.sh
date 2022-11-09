echo "Getting Cosmos IP Address"
CosmosContainerIpAddress=`ping cosmos -c1 | head -1 | grep -Eo '[0-9.]{4,}'`
CosmosEndpointURI=`echo "https://$CosmosContainerIpAddress:8081"`

echo "Installing the cosmos emulator cert locally"
curl -k $CosmosEndpointURI/_explorer/emulator.pem > ~/emulatorcert.crt
cp ~/emulatorcert.crt /usr/local/share/ca-certificates/
update-ca-certificates

echo "Persisting settings to appSettings.json"
touch appSettings.json
echo '{}' > appSettings.json

json -I -f appSettings.json \
      -e "this['ApplicationInsightsSettings:ConnectionString']='$ApplicationInsightsSettings_ConnectionString'"

json -I -f appSettings.json \
      -e "this['AvalaraSettings:AccountID']='$AvalaraSettings_AccountID'" \
      -e "this['AvalaraSettings:CompanyCode']='$AvalaraSettings_CompanyCode'" \
      -e "this['AvalaraSettings:CompanyID']='$AvalaraSettings_CompanyID'" \
      -e "this['AvalaraSettings:LicenseKey']='$AvalaraSettings_LicenseKey'" \
      -e "this['AvalaraSettings:BaseApiUrl']='$AvalaraSettings_BaseApiUrl'"

json -I -f appSettings.json \
      -e "this['CardConnectSettings:Authorization']='$CardConnectSettings_Authorization'" \
      -e "this['CardConnectSettings:AuthorizationCad']='$CardConnectSettings_AuthorizationCad'" \
      -e "this['CardConnectSettings:BaseUrl']='$CardConnectSettings_BaseUrl'" \
      -e "this['CardConnectSettings:CadMerchantID']='$CardConnectSettings_CadMerchantID'" \
      -e "this['CardConnectSettings:Site']='$CardConnectSettings_Site'" \
      -e "this['CardConnectSettings:UsdMerchantID']='$CardConnectSettings_UsdMerchantID'"

json -I -f appSettings.json \
      -e "this['CosmosSettings:DatabaseName']='$CosmosSettings_DatabaseName'" \
      -e "this['CosmosSettings:EndpointUri']='$CosmosSettings_EndpointUri'" \
      -e "this['CosmosSettings:PrimaryKey']='$CosmosSettings_PrimaryKey'" \
      -e "this['CosmosSettings:RequestTimeoutInSeconds']='$CosmosSettings_RequestTimeoutInSeconds'"

json -I -f appSettings.json \
      -e "this['EasyPostSettings:APIKey']='$EasyPostSettings_APIKey'" \
      -e "this['EasyPostSettings:CustomsSigner']='$EasyPostSettings_CustomsSigner'" \
      -e "this['EasyPostSettings:FedexAccountId']='$EasyPostSettings_FedexAccountId'" \
      -e "this['EasyPostSettings:FreeShippingTransitDays']='$EasyPostSettings_FreeShippingTransitDays'" \
      -e "this['EasyPostSettings:NoRatesFallbackCost']='$EasyPostSettings_NoRatesFallbackCost'" \
      -e "this['EasyPostSettings:NoRatesFallbackTransitDays']='$EasyPostSettings_NoRatesFallbackTransitDays'" \
      -e "this['EasyPostSettings:USPSAccountId']='$EasyPostSettings_USPSAccountId'"

json -I -f appSettings.json \
      -e "this['EnvironmentSettings:BuildNumber']='$EnvironmentSettings_BuildNumber'" \
      -e "this['EnvironmentSettings:Commit']='$EnvironmentSettings_Commit'" \
      -e "this['EnvironmentSettings:Environment']='$EnvironmentSettings_Environment'" \
      -e "this['EnvironmentSettings:AddressValidationProvider']='$EnvironmentSettings_AddressValidationProvider'" \
      -e "this['EnvironmentSettings:CMSProvider']='$EnvironmentSettings_CMSProvider'" \
      -e "this['EnvironmentSettings:CurrencyConversionProvider']='$EnvironmentSettings_CurrencyConversionProvider'" \
      -e "this['EnvironmentSettings:EmailServiceProvider']='$EnvironmentSettings_EmailServiceProvider'" \
      -e "this['EnvironmentSettings:OMSProvider']='$EnvironmentSettings_OMSProvider'" \
      -e "this['EnvironmentSettings:PaymentProvider']='$EnvironmentSettings_PaymentProvider'" \
      -e "this['EnvironmentSettings:ShippingProvider']='$EnvironmentSettings_ShippingProvider'" \
      -e "this['EnvironmentSettings:TaxProvider']='$EnvironmentSettings_TaxProvider'"

json -I -f appSettings.json \
      -e "this['FlurlSettings:TimeoutInSeconds']='$FlurlSettings_TimeoutInSeconds'"

json -I -f appSettings.json \
      -e "this['JobSettings:CaptureCreditCardsAfterDate']='$JobSettings_CaptureCreditCardsAfterDate'" \
      -e "this['JobSettings:ShouldCaptureCreditCardPayments']='$JobSettings_ShouldCaptureCreditCardPayments'" \
      -e "this['JobSettings:ShouldRunZoho']='$JobSettings_ShouldRunZoho'"

json -I -f appSettings.json \
      -e "this['OrderCloudSettings:ApiUrl']='$OrderCloudSettings_ApiUrl'" \
      -e "this['OrderCloudSettings:ClientIDsWithAPIAccess']='$OrderCloudSettings_ClientIDsWithAPIAccess'" \
      -e "this['OrderCloudSettings:IncrementorPrefix']='$OrderCloudSettings_IncrementorPrefix'" \
      -e "this['OrderCloudSettings:MarketplaceID']='$OrderCloudSettings_MarketplaceID'" \
      -e "this['OrderCloudSettings:MarketplaceName']='$OrderCloudSettings_MarketplaceName'" \
      -e "this['OrderCloudSettings:MiddlewareClientID']='$OrderCloudSettings_MiddlewareClientID'" \
      -e "this['OrderCloudSettings:MiddlewareClientSecret']='$OrderCloudSettings_MiddlewareClientSecret'" \
      -e "this['OrderCloudSettings:WebhookHashKey']='$OrderCloudSettings_WebhookHashKey'"

json -I -f appSettings.json \
      -e "this['SendGridSettings:ApiKey']='$SendGridSettings_ApiKey'" \
      -e "this['SendgridSettings:BillingEmail']='$SendgridSettings_BillingEmail'" \
      -e "this['SendgridSettings:CriticalSupportEmails']='$SendgridSettings_CriticalSupportEmails'" \
      -e "this['SendgridSettings:CriticalSupportTemplateID']='$SendgridSettings_CriticalSupportTemplateID'"
      -e "this['SendgridSettings:FromEmail']='$SendgridSettings_FromEmail'" \
      -e "this['SendgridSettings:LineItemStatusChangeTemplateID']='$SendgridSettings_LineItemStatusChangeTemplateID'" \
      -e "this['SendgridSettings:NewUserTemplateID']='$SendgridSettings_NewUserTemplateID'" \
      -e "this['SendgridSettings:OrderApprovalTemplateID']='$SendgridSettings_OrderApprovalTemplateID'" \
      -e "this['SendgridSettings:OrderSubmitTemplateID']='$SendgridSettings_OrderSubmitTemplateID'" \
      -e "this['SendgridSettings:PasswordResetTemplateID']='$SendgridSettings_PasswordResetTemplateID'" \
      -e "this['SendgridSettings:ProductInformationRequestTemplateID']='$SendgridSettings_ProductInformationRequestTemplateID'" \
      -e "this['SendgridSettings:QuoteOrderSubmitTemplateID']='$SendgridSettings_QuoteOrderSubmitTemplateID'" \
      -e "this['SendgridSettings:SupportCaseEmail']='$SendgridSettings_SupportCaseEmail'" \

json -I -f appSettings.json \
      -e "this['ServiceBusSettings:ConnectionString']='$ServiceBusSettings_ConnectionString'" \
      -e "this['ServiceBusSettings:ZohoQueueName']='$ServiceBusSettings_ZohoQueueName'"

json -I -f appSettings.json \
      -e "this['SmartyStreetSettings:AuthID']='$SmartyStreetSettings_AuthID'" \
      -e "this['SmartyStreetSettings:AuthToken']='$SmartyStreetSettings_AuthToken'" \

json -I -f appSettings.json \
      -e "this['StorageAccountSettings:ConnectionString']='$StorageAccountSettings_ConnectionString'" \
      -e "this['StorageAccountSettings:ContainerNameCache']='$StorageAccountSettings_ContainerNameCache'" \
      -e "this['StorageAccountSettings:ContainerNameExchangeRates']='$StorageAccountSettings_ContainerNameExchangeRates'" \
      -e "this['StorageAccountSettings:ContainerNameTranslations']='$StorageAccountSettings_ContainerNameTranslations'" \
      -e "this['StorageAccountSettings:ContainerNameQueue']='$StorageAccountSettings_ContainerNameQueue'" \
      -e "this['StorageAccountSettings:HostUrl']='$StorageAccountSettings_HostUrl'" \
      -e "this['StorageAccountSettings:Key']='$StorageAccountSettings_Key'" \
      
json -I -f appSettings.json \
      -e "this['UI:BaseAdminUrl']='$UI_BaseAdminUrl'"

json -I -f appSettings.json \
      -e "this['ZohoSettings:AccessToken']='$ZohoSettings_AccessToken'" \
      -e "this['ZohoSettings:ApiUrl']='$ZohoSettings_ApiUrl'" \
      -e "this['ZohoSettings:ClientId']='$ZohoSettings_ClientId'" \
      -e "this['ZohoSettings:ClientSecret']='$ZohoSettings_ClientSecret'" \
      -e "this['ZohoSettings:OrganizationID']='$ZohoSettings_OrganizationID'" \
      -e "this['ZohoSettings:PerformOrderSubmitTasks']='$ZohoSettings_PerformOrderSubmitTasks'"

echo "Run Middleware"
dotnet Headstart.API.dll