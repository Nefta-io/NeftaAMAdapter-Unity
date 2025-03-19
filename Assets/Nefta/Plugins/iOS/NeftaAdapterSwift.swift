//
//  NeftaAdapterSwift.swift
//  AMAdapter
//
//  Created by Tomaz Treven on 16. 3. 25.
//  Copyright © 2025 Google. All rights reserved.
//
import GoogleMobileAds
import NeftaSDK

class NeftaAdapterSwift {
    enum AdTypeSwift : Int{
        case Other = 0
        case Banner = 1
        case Interstitial = 2
        case Rewarded = 3
    }
    
    static func OnExternalMediationRequestLoad(_ adType: AdTypeSwift, requestedFloorPrice: Float64, calculatedFloorPrice: Float64, adUnitId: String) {
        NeftaPlugin.OnExternalMediationRequest("am", adType: adType.rawValue, requestedFloorPrice: requestedFloorPrice, calculatedFloorPrice: calculatedFloorPrice, adUnitId: adUnitId, revenue: -1, precision: nil, status: 1)
    }
    
    static func OnExternalMediationRequestFail(_ adType: AdTypeSwift, requestedFloorPrice: Float64, calculatedFloorPrice: Float64, adUnitId: String?, error: NSError?) {
        var status = 0
        if let e = error, e.code == GADErrorCode.noFill.rawValue || e.code == GADErrorCode.mediationNoFill.rawValue {
            status = 2
        }
        NeftaPlugin.OnExternalMediationRequest("am", adType: adType.rawValue, requestedFloorPrice: requestedFloorPrice, calculatedFloorPrice: calculatedFloorPrice, adUnitId: adUnitId, revenue: -1, precision: nil, status: status)
    }
    
    static func OnExternalMediationImpression(_ adValue: GADAdValue) {
        let data = NSMutableDictionary()
        data["value"] = adValue.value
        data["precision"] = adValue.precision
        data["currency_code"] = adValue.currencyCode
        NeftaPlugin.OnExternalMediationImpression("am", data: data)
    }
}
