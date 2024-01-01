//
//  GameView.swift
//  Ryujinx
//
//  Created by Isaac Marovitz on 31/12/2023.
//

import SwiftUI

struct GameView: View {
    @State var imageName: String
    @State var title: String

    var body: some View {
        VStack {
            Image(imageName)
                .resizable()
                .frame(width: 80, height: 80)
                .clipShape(RoundedRectangle(cornerRadius: 5))
            Text(title)
                .multilineTextAlignment(.center)
                .lineLimit(2, reservesSpace: true)
                .truncationMode(.tail)
        }
        .padding(5)
    }
}

#Preview {
    GameView(imageName: "BOTW", title: "The Legend of Zelda: Breath of the Wild")
}
