//
//  RyujinxSettings.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 01/01/2024.
//

import Foundation
import LibRyujinx

public class RyujinxSettings: Codable {
    var region: RegionCode = .USA
    var language: SystemLanguage = .AmericanEnglish
    var ignoreMissingServices: Bool = false

    var shaderCache: Bool = true
    var textureRecomp: Bool = true
    var colorSpacePassthrough: Bool = true

    var touchInput: Bool = false
    var motionControls: Bool = false
    var onscreenController: Bool = true

    public init() {}

    required public init(from decoder: Decoder) throws {
        let container = try decoder.container(keyedBy: CodingKeys.self)

        self.region = try container.decodeIfPresent(RegionCode.self, forKey: .region) ?? .USA
        self.language = try container.decodeIfPresent(SystemLanguage.self, forKey: .language) ?? .AmericanEnglish
        self.ignoreMissingServices = try container.decodeIfPresent(Bool.self, forKey: .ignoreMissingServices) ?? false

        self.shaderCache = try container.decodeIfPresent(Bool.self, forKey: .shaderCache) ?? true
        self.textureRecomp = try container.decodeIfPresent(Bool.self, forKey: .textureRecomp) ?? true
        self.colorSpacePassthrough = try container.decodeIfPresent(Bool.self, forKey: .colorSpacePassthrough) ?? true

        self.touchInput = try container.decodeIfPresent(Bool.self, forKey: .touchInput) ?? false
        self.motionControls = try container.decodeIfPresent(Bool.self, forKey: .motionControls) ?? false
        self.onscreenController = try container.decodeIfPresent(Bool.self, forKey: .onscreenController) ?? true
    }

    @discardableResult
    public static func decode() -> RyujinxSettings {
        do {
            let documentsUrl = try FileManager.default.url(for: .documentDirectory, in: .allDomainsMask, appropriateFor: nil, create: true)
            let settingsUrl = documentsUrl.appending(path: "Settings").appendingPathExtension("plist")

            let decoder = PropertyListDecoder()
            let settings = try decoder.decode(RyujinxSettings.self, from: Data(contentsOf: settingsUrl))
            settings.encode()
            return settings
        } catch {
            return RyujinxSettings()
        }
    }

    public func encode() {
        do {
            let documentsUrl = try FileManager.default.url(for: .documentDirectory, in: .allDomainsMask, appropriateFor: nil, create: true)
            let settingsUrl = documentsUrl.appending(path: "Settings").appendingPathExtension("plist")

            let encoder = PropertyListEncoder()
            encoder.outputFormat = .xml
            let data = try encoder.encode(self)
            try data.write(to: settingsUrl)
        } catch {
            print("Failed to save settings \(error)!")
        }
    }

    public func getGraphicsConfig() -> GraphicsConfiguration {
        return GraphicsConfiguration(
            ResScale: 1.0,
            MaxAnisotropy: -1.0,
            FastGpuTime: true,
            Fast2DCopy: true,
            EnableMacroJit: false,
            EnableMacroHLE: true,
            EnableShaderCache: self.shaderCache,
            EnableTextureRecompression: self.textureRecomp,
            BackendThreading: BackendThreading.Auto,
            AspectRatio: AspectRatio.Fixed16x9)
    }
}
