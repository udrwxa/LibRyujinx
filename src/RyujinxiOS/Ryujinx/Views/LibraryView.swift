//
//  LibraryView.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI
#if !targetEnvironment(simulator)
import LibRyujinx
#endif

struct LibraryView: View {
    @EnvironmentObject var settings: Settings

    @State var search: String = ""
    @State var games: [Game] = []
    @State var isFirmwareInstalled: Bool = false

    private let gridLayout = [GridItem(.adaptive(minimum: 100))]
    private var searchedGames: [Game] {
        guard !search.isEmpty else { return games }
        return games.filter({ $0.titleName.localizedCaseInsensitiveContains(search) })
    }

    var body: some View {
        NavigationStack {
            Group {
                if games.isEmpty {
                    VStack {
                        Spacer()
                        Group {
                            Text("No games installed.\n Press the ") + Text(Image(systemName: "plus")) + Text(" button to add some.")
                        }
                            .multilineTextAlignment(.center)
                        Spacer()
                    }
                } else {
                    ScrollView {
                        LazyVGrid(columns: gridLayout, spacing: 10) {
                            ForEach(searchedGames, id: \.id) { game in
                                GameView(game: game)
                            }
                        }
                        .padding(.horizontal)
                    }
                }
            }
            .toolbar {
                ToolbarItemGroup(placement: .topBarLeading) {
                    Text("Library")
                        .font(.headline)
                        .padding(.leading, 5)
                }
                ToolbarItemGroup(placement: .primaryAction) {
                    Button {
                        print("Add a game")
                    } label: {
                        Image(systemName: "plus")
                            .foregroundStyle(.primary)
                    }
                }
                ToolbarItemGroup(placement: .secondaryAction) {
                    Button {
                        print("Open Mii Editor")
                    } label: {
                        Label {
                            Text("Open Mii Editor")
                        } icon: {
                            Image(systemName: "person")
                        }
                    }
                    .disabled(!isFirmwareInstalled)
                }
            }
            .navigationBarTitleDisplayMode(.inline)
            .searchable(text: $search, placement: .navigationBarDrawer(displayMode: .always))
        }
        .animation(.easeInOut(duration: 0.2), value: searchedGames)
        .animation(.easeInOut(duration: 0.2), value: search)
        .onAppear {
            checkFirmware()
        }
    }

    func checkFirmware() {
#if !targetEnvironment(simulator)
        let cstring = device_get_installed_firmware_version()
        let version = String(cString: cstring!)
        if version != String() {
            isFirmwareInstalled = true
        }
#endif
    }
}

#Preview {
    LibraryView()
}
