//
//  RyujinxApp.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI
#if !targetEnvironment(simulator)
import LibRyujinx
#endif

@main
struct RyujinxApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
                .onAppear {
                    let documentsDirectory = try! FileManager.default.url(for: .documentDirectory, in: .userDomainMask, appropriateFor: nil, create: true)
#if !targetEnvironment(simulator)
                    initialize(strdup(documentsDirectory.path(percentEncoded: false)))
#endif
                }
        }
    }
}
