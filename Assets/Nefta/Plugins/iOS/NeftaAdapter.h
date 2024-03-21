#import <Foundation/Foundation.h>
#import <GoogleMobileAds/GoogleMobileAds.h>

#import "NeftaExtras.h"
#import <NeftaSDK/NeftaSDK-Swift.h>

typedef NS_ENUM(NSInteger, NeftaAdapterErrorCode) {
    NeftaAdapterErrorCodeInvalidServerParameters = 101,
    NeftaAdapterErrorCodeAdNotReady = 102,
};

@interface NeftaAdapter : NSObject <GADMediationAdapter>
@property NSString *ErrorDomain;
@property NeftaPlugin_iOS *Plugin;
@end
