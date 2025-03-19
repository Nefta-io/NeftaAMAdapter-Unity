#import "NeftaBanner.h"

@implementation NeftaBanner
+ (instancetype)Init:(NSString *)id listener:(nonnull GADMediationBannerLoadCompletionHandler)listener errorDomain:(NSString *)errorDomain {
    NeftaBanner *instance = [[self alloc] init];
    instance.banner = [[NBanner alloc] initWithId: id position: PositionNone];
    instance.listener = listener;
    instance.errorDomain = errorDomain;
    instance.banner._listener = instance;
    return instance;
}

- (void)Load {
    [_banner Load];
}

- (void)OnLoadFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    _listener(nil, [NSError errorWithDomain: error._message code: error._code userInfo: nil]);
}

- (void)OnLoadWithAd:(NAd * _Nonnull)ad width:(NSInteger)width height:(NSInteger)height {
    ((NBanner *)ad)._onRemove = ^(NBanner * _Nonnull banner) {
        [banner Close];
    };
    (void)[_banner GracefulShow: nil];
    _adEventDelegate = _listener(self, nil);
}

- (void)OnShowFailWithAd:(NAd * _Nonnull)ad error:(NError * _Nonnull)error {
    NSDictionary *userInfo = @ { NSLocalizedDescriptionKey: error._message };
    [_adEventDelegate didFailToPresentWithError: [NSError errorWithDomain: _errorDomain code: error._code userInfo: userInfo]];
}

- (void)OnShowWithAd:(NAd * _Nonnull)ad {
    [_adEventDelegate reportImpression];
}

- (void)OnClickWithAd:(NAd * _Nonnull)ad {
    [_adEventDelegate reportClick];
}

- (void)OnCloseWithAd:(NAd * _Nonnull)ad {
}

- (nonnull UIView *)view {
    return [_banner GetView];
}

@end
