#import <Foundation/Foundation.h>
#import "NeftaAdapter.h"
#import <NeftaSDK/NeftaSDK-Swift.h>

@protocol NeftaRequest <NSObject>

@property NeftaAdapter *_adapter;
@property Placement *_placement;
@property NSString *_placementId;
@property int _state;

- (void) OnLoadFail: (NSString *)error;
- (void) OnLoad: (Placement *)placement;
- (void) OnShow;
- (void) OnClick;
- (void) OnRewarded;
- (void) OnClose;

@end
