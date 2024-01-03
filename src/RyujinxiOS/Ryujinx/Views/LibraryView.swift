//
//  LibraryView.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI

struct LibraryView: View {
    @EnvironmentObject var settings: Settings

    @State var search: String = ""
    @State var games: [Game] = []

    private let gridLayout = [GridItem(.adaptive(minimum: 100))]

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
                            ForEach(games, id: \.id) { game in
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
                }
            }
            .navigationBarTitleDisplayMode(.inline)
            .searchable(text: $search, placement: .navigationBarDrawer(displayMode: .always))
        }
    }
}

#Preview {
    LibraryView()
}
