//
//  SettingsView.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI
import LibRyujinx

struct SettingsView: View {
    @EnvironmentObject var settings: Settings

    @State var useGrid: Bool = true
    @State var firmwareVersion: String = "N/A"
    @State var areKeysInstalled: Bool = false
    @State var isFirmwareInstalled: Bool = false
    @State var showingKeyImport = false
    @State var showingFimrwareImport = false

    @State var keysUrl: URL?

    var body: some View {
        Form {
            Section("App") {
                Toggle("Use Grid", isOn: $useGrid)
                    .disabled(true)
                HStack {
                    Text("System Firmware")
                    Spacer()
                    Text(firmwareVersion)
                        .foregroundStyle(isFirmwareInstalled ? .primary : .secondary)
                }
                Button("Import Keys") {
                    showingKeyImport.toggle()
                }.fileImporter(isPresented: $showingKeyImport, allowedContentTypes: [.data], allowsMultipleSelection: false) { result in
                    switch result {
                    case .success(let urls):
                        do {
                            try urls.forEach { url in
                                let gotAccess = url.startAccessingSecurityScopedResource()
                                if !gotAccess {
                                    print("Failed to get access to imported keys at \(url)!")
                                    return
                                }

                                if FileManager.default.fileExists(atPath: keysUrl!.path(percentEncoded: false)) {
                                    try FileManager.default.removeItem(at: keysUrl!)
                                }

                                try FileManager.default.copyItem(at: url, to: keysUrl!)

                                url.stopAccessingSecurityScopedResource()
                            }
                        } catch {
                            print(error)
                        }
                    case .failure(let error):
                        print(error)
                    }

                    checkKeys()
                }
                Button("Import Firmware") {
                    showingFimrwareImport.toggle()
                }.fileImporter(isPresented: $showingFimrwareImport, allowedContentTypes: [.zip, .folder], allowsMultipleSelection: false) { result in
                    switch result {
                    case .success(let urls):
                        do {
                            try urls.forEach { url in
                                let gotAccess = url.startAccessingSecurityScopedResource()
                                if !gotAccess {
                                    print("Failed to get access to imported firmware at \(url)!")
                                    return
                                }

                                let handle = try FileHandle(forUpdating: url)
                                device_install_firmware(handle.fileDescriptor, false)

                                try handle.close()
                                url.stopAccessingSecurityScopedResource()
                                checkFirmware()
                            }
                        } catch {
                            print(error)
                        }
                    case .failure(let error):
                        print(error)
                    }
                }
                .disabled(!areKeysInstalled)
            }
            Section("System") {
                Picker("System Region", selection: $settings.settings.region) {
                    ForEach(RegionCode.allCases, id: \.self) { region in
                        Text(region.rawValue).id(region)
                    }
                }
                Picker("System Language", selection: $settings.settings.language) {
                    ForEach(SystemLanguage.allCases, id: \.self) { lang in
                        Text(lang.rawValue).id(lang)
                    }
                }
                Toggle("Ignore Missing Services", isOn: $settings.settings.ignoreMissingServices)
            }
            Section("Graphics") {
                Toggle("Enable Shader Cache", isOn: $settings.settings.shaderCache)
                Toggle("Enable Texture Recompression", isOn: $settings.settings.textureRecomp)
                Toggle("Color Space Passthrough", isOn: $settings.settings.colorSpacePassthrough)
            }
            Section("Input") {
                Toggle("Enable Touchscreen Input", isOn: $settings.settings.touchInput)
                Toggle("Enable Motion Controls", isOn: $settings.settings.motionControls)
                Toggle("Enable Onscreen Controller", isOn: $settings.settings.onscreenController)
            }
        }
        .formStyle(.grouped)
        .onAppear {
            checkKeys()
            checkFirmware()
        }
    }

    func checkKeys() {
        do {
            let documentsUrl = try FileManager.default.url(for: .documentDirectory, in: .userDomainMask, appropriateFor: nil, create: true)
            keysUrl = documentsUrl.appending(path: "system").appending(path: "prod").appendingPathExtension("keys")
            areKeysInstalled = FileManager.default.fileExists(atPath: keysUrl!.path(percentEncoded: false))
        } catch {
            print(error)
        }
    }

    func checkFirmware() {
        let cstring = device_get_installed_firmware_version()
        let version = String(cString: cstring!)
        if version != String() {
            isFirmwareInstalled = true
            firmwareVersion = version
        }
    }
}

#Preview {
    SettingsView()
        .environmentObject(Settings())
}
