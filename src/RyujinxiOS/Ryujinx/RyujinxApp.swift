//
//  RyujinxApp.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI
import LibRyujinx

@main
struct RyujinxApp: App {
    var body: some Scene {
        WindowGroup {
            ContentView()
                .onAppear {
                    let documentsDirectory = try! FileManager.default.url(for: .documentDirectory, in: .userDomainMask, appropriateFor: nil, create: true)
                    initialize(strdup(documentsDirectory.path(percentEncoded: false)))
                }
        }
    }
}
