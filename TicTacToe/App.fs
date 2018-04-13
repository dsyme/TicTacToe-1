﻿namespace TicTacToe

open Elmish
open Elmish.XamarinForms
open Xamarin.Forms

/// Represents a player and a player's move
type Player = 
    | X 
    | O 
    member p.Swap = match p with X -> O | O -> X

/// Represents the game state contents of a single cell
type GameCell = 
    | Empty 
    | Full of Player
    member x.CanPlay = (x = Empty)

/// Represents the result of a game
type GameResult = 
    | StillPlaying 
    | XWins 
    | OWins 
    | Draw

/// Represents a position on the board
type Pos = int * int

/// Represents an update to the game
type Msg =
    | Play of Pos
    | Restart

/// Represents the state of the game board
type Board = Map<Pos, GameCell>

/// Represents the elements of a possibly-winning row
type Row = GameCell list

/// Represents the state of the game
type Model =
    { 
      NextUp: Player
      Board: Board
    }

/// The model, update and view content of the app. This is placed in an 
/// independent model to facilitate unit testing.
module App = 
    open System.Windows.Input
    open System.Runtime.CompilerServices

    let positions = 
        [ for x in 0 .. 2 do 
            for y in 0 .. 2 do 
               yield (x, y) ]

    let initialBoard = 
        Map.ofList [ for p in positions -> p, Empty ]

    let init () = 
        { NextUp = X
          Board = initialBoard }

    /// Check if there are any more moves available in the game
    let anyMoreMoves m = m.Board |> Map.exists (fun _ c -> c = Empty)
    
    let getWinLines () =
        [
            // rows
            for row in 0 .. 2 do yield [(row,0); (row,1); (row,2)]
            // columns
            for col in 0 .. 2 do yield [(0,col); (1,col); (2,col)]
            // diagonals
            yield [(0,0); (1,1); (2,2)]
            yield [(0,2); (1,1); (2,0)]
        ]

    /// Determine if a line is a winning line.
    let getLineWinner (cells: Board) line =
        if line |> List.forall (fun p -> match cells.[p] with Full X -> true | _ -> false) then  XWins
        elif line |> List.forall (fun p -> match cells.[p] with Full O -> true | _ -> false) then  OWins
        else StillPlaying

    /// Determine the game result, if any.
    let getGameResult model =
        let winLines = getWinLines () |> Seq.map (getLineWinner model.Board)

        let xWins = winLines |> Seq.tryFind (fun r -> r = XWins)
        match xWins with
        | Some p -> p
        | _ -> 
        let oWins = winLines |> Seq.tryFind (fun r -> r = OWins)
        match oWins with         
        | Some p -> p 
        | _ -> 

        match anyMoreMoves model with
        | true -> StillPlaying
        | false -> Draw


    /// Get a message to show the current game result
    let getMessage model = 
        match getGameResult model with 
        | StillPlaying -> sprintf "%O's turn" model.NextUp
        | XWins -> "X wins!"
        | OWins -> "O Wins!"
        | Draw -> "It is a draw!"

    /// The 'update' function to update the model
    let update gameOver msg model =
        let newModel = 
            match msg with
            | Play pos -> { model with Board = model.Board.Add(pos, Full model.NextUp); NextUp = model.NextUp.Swap }
            | Restart -> init()

        // Make an announcement in the middle of the game. 
        let result = getGameResult newModel
        if result <> StillPlaying then gameOver (getMessage newModel)

        // Return the new model.
        newModel

    /// A helper used in the 'view' function to get the name 
    /// of the Xaml resource for the image for a player
    let imageForPos cell =
        match cell with
        | Full X -> "Cross"
        | Full O -> "Nought"
        | Empty -> ""

    /// A helper to get the suffix used in the Xaml for a position on the board.
    let uiText (row,col) = 
        (match row with 0 -> "T" | 1 -> "M" | 2 -> "B" | _ -> failwith "huh?") + 
        (match col with 0 -> "L" | 1 -> "C" | 2 -> "R" | _ -> failwith "huh?")

    /// A condition used in the 'view' function to check if we can play in a cell.
    /// The visual contents of a cell depends on this condition.
    let canPlay model cell =
         match cell with 
         | Full _ -> false
         | Empty -> 
         match getGameResult model with
         | StillPlaying -> true
         | _ -> false

    /// The 'view' function giving the Xaml bindings from the model to the view
    let view (model: Model) dispatch =
        rows 
            [ rowdef "*"; rowdef "auto"; rowdef "auto" ]
            [ grid 
                [ rowdef "*"; rowdef 5.0; rowdef "*"; rowdef 5.0; rowdef "*" ]
                [ coldef "*"; coldef 5.0; coldef "*"; coldef 5.0; coldef "*" ]
                [ yield rect Color.Black @@ gridRow 1
                  yield rect Color.Black @@ gridRow 3 
                  yield rect Color.Black @@ gridCol 1
                  yield rect Color.Black @@ gridCol 3
                  for ((row,col) as pos) in positions do 
                      let x = 
                          if canPlay model model.Board.[pos] then 
                              button (fun () -> dispatch (Play pos)) :> View
                          else
                              imageResource (imageForPos model.Board.[pos]) :> _
                          |> withMargin 5.0
                      yield x @@ gridLoc (row*2) (col*2) ]

                |> withRowSpacing 0.0
                |> withColumnSpacing 0.0
                |> withHorizontalOptions LayoutOptions.Center
                |> withVerticalOptions LayoutOptions.Center

              label (getMessage model) 
                 |> withMargin 10.0
                 |> withLabelTextColor Color.Black
                 |> withHorizontalTextAlignment TextAlignment.Center

              button (fun () -> dispatch Restart)
                 |> withText("Restart game")
                 |> withBackgroundColor(Color.LightBlue)
                 |> withButtonTextColor(Color.Black)  ]

                   //,FontSize=(FontSizeConverter().ConvertFromInvariantString "Large") :> float)


/// Stitch the model, update and view content into a single app.
type App() =
    inherit Application()

    // Display a modal message giving the game result. This is doing a UI
    // action in the model update, which is ok for modal messages. We factor
    // this dependency out to allow unit testing of the 'update' function. 
    let gameOver msg =
        Application.Current.MainPage.DisplayAlert("Game over", msg, "OK") |> ignore

    let page = 
        Program.mkSimple App.init (App.update gameOver) (fun _ _ -> (HelperPage(), App.view), []) 
        |> Program.withConsoleTrace
        |> Program.run
        
    let mainPage = NavigationPage(page, BarBackgroundColor = Color.LightBlue, BarTextColor = Color.Black)
    do base.MainPage <- mainPage

