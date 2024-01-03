//
//  LibraryView.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI
import UniformTypeIdentifiers
#if !targetEnvironment(simulator)
import LibRyujinx
#endif

struct LibraryView: View {
    @EnvironmentObject var settings: Settings
    @EnvironmentObject var games: Games

    @State var search: String = ""
    @State var isFirmwareInstalled: Bool = false
    @State var showingGameImport = false

    private let gridLayout = [GridItem(.adaptive(minimum: 100))]
    private var searchedGames: [Game] {
        guard !search.isEmpty else { return games.games }
        return games.games.filter({ $0.titleName.localizedCaseInsensitiveContains(search) })
    }

    var body: some View {
        NavigationStack {
            Group {
                if games.games.isEmpty {
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
                                    .environmentObject(games)
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
                        showingGameImport.toggle()
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
            .fileImporter(isPresented: $showingGameImport, allowedContentTypes: [.nca, .nro, .nso, .nsp], allowsMultipleSelection: false) { result in
                switch result {
                case .success(let urls):
                    do {
                        try urls.forEach { url in
                            let gotAccess = url.startAccessingSecurityScopedResource()
                            if !gotAccess {
                                print("Failed to get access to imported game at \(url)!")
                                return
                            }

                            // Check if the game is valid before copying it
                            let handle = try FileHandle(forReadingFrom: url)
                            #if !targetEnvironment(simulator)
                            // Something something LibRyujinx
                            #endif
                            try handle.close()

                            let documentsFolder = try FileManager.default.url(for: .documentDirectory, in: .userDomainMask, appropriateFor: nil, create: true)
                            let gameFiles = documentsFolder.appending(path: "GameFiles")

                            // We can't use title ID as a unique file name as many homebrew just have all 0s
                            let gameFolder = gameFiles.appending(path: UUID().uuidString)

                            if !FileManager.default.fileExists(atPath: gameFolder.path(percentEncoded: false)) {
                                try FileManager.default.createDirectory(at: gameFolder, withIntermediateDirectories: true)
                            }

                            let finalLocation = gameFolder.appending(path: "Title").appendingPathExtension(url.pathExtension)

                            if FileManager.default.fileExists(atPath: finalLocation.path(percentEncoded: false)) {
                                try FileManager.default.removeItem(at: finalLocation)
                            }

                            try FileManager.default.copyItem(at: url, to: finalLocation)

                            url.stopAccessingSecurityScopedResource()
                            loadGames()
                        }
                    } catch {
                        print(error)
                    }
                case .failure(let error):
                    print(error)
                }
            }
        }
        .animation(.easeInOut(duration: 0.2), value: searchedGames)
        .animation(.easeInOut(duration: 0.2), value: search)
        .onAppear {
            loadGames()
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

    func loadGames() {
        games.games.removeAll()

        do {
            let documentsFolder = try FileManager.default.url(for: .documentDirectory, in: .userDomainMask, appropriateFor: nil, create: true)
            let gameFiles = documentsFolder.appending(path: "GameFiles")

            let gameEnumerator = FileManager.default.enumerator(at: gameFiles, 
                                                                includingPropertiesForKeys: [.isDirectoryKey],
                                                                options: [.skipsSubdirectoryDescendants])
            while let url = gameEnumerator?.nextObject() as? URL {
                var type: UTType = .nsp
                let filesEnumerator = FileManager.default.enumerator(at: url,
                                                                     includingPropertiesForKeys: [.isRegularFileKey])
                
                // TODO: This works but it's spaghetti and I don't like it
                while let url = filesEnumerator?.nextObject() as? URL {
                    let urlExtension = url.pathExtension
                    let url = url.deletingPathExtension()
                    if url.lastPathComponent == "Title" {
                        type = UTType(filenameExtension: urlExtension) ?? .nsp
                    }
                }

                games.games.append(Game(containerFolder: url,
                                        fileType: type,
                                        titleName: url.lastPathComponent,
                                        titleId: "",
                                        developer: "",
                                        version: ""))
            }
        } catch {
            print(error)
        }
    }
}

#Preview {
    LibraryView()
}
