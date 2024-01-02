//
//  SettingsView.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI
import LibRyujinx

struct SettingsView: View {
    @State var useGrid: Bool = true
    @State var firmwareVersion: String = "N/A"
    @State var isFirmwareInstalled: Bool = false
    @State var showingKeyImport = false
    @State var showingFimrwareImport = false

    @State var region: RegionCode = .USA
    @State var language: SystemLanguage = .AmericanEnglish
    @State var ignoreMissingServices: Bool = false

    @State var shaderCache: Bool = true
    @State var textureRecomp: Bool = true
    @State var colorSpacePassthrough: Bool = true

    @State var touchInput: Bool = false
    @State var motionControls: Bool = false
    @State var onscreenController: Bool = true

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
                }.fileImporter(isPresented: $showingKeyImport, allowedContentTypes: [.text], allowsMultipleSelection: false) { result in
                    switch result {
                    case .success(let url):
                        print(url)
                    case .failure(let error):
                        print(error)
                    }
                }
                Button("Import Firmware") {
                    showingFimrwareImport.toggle()
                }.fileImporter(isPresented: $showingFimrwareImport, allowedContentTypes: [.zip, .folder], allowsMultipleSelection: false) { result in
                    switch result {
                    case .success(let url):
                        print(url)
                    case .failure(let error):
                        print(error)
                    }
                }
            }
            Section("System") {
                Picker("System Region", selection: $region) {
                    ForEach(RegionCode.allCases, id: \.self) { region in
                        Text(region.rawValue).id(region)
                    }
                }
                Picker("System Language", selection: $language) {
                    ForEach(SystemLanguage.allCases, id: \.self) { lang in
                        Text(lang.rawValue).id(lang)
                    }
                }
                Toggle("Ignore Missing Services", isOn: $ignoreMissingServices)
            }
            Section("Graphics") {
                Toggle("Enable Shader Cache", isOn: $shaderCache)
                Toggle("Enable Texture Recompression", isOn: $textureRecomp)
                Toggle("Color Space Passthrough", isOn: $colorSpacePassthrough)
            }
            Section("Input") {
                Toggle("Enable Touchscreen Input", isOn: $touchInput)
                Toggle("Enable Motion Controls", isOn: $motionControls)
                Toggle("Enable Onscreen Controller", isOn: $onscreenController)
            }
        }
        .formStyle(.grouped)
        .onAppear {
            let cstring = device_get_installed_firmware_version()
            let version = String(cString: cstring!)
            if version != String() {
                isFirmwareInstalled = true
                firmwareVersion = version
            }
        }
    }
}

#Preview {
    SettingsView()
}
