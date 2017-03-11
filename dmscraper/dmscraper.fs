module DmScraper

open FSharp.Data
open System
open System.IO
open System.Threading
open System.Text.RegularExpressions

// type BookHtml = HtmlProvider< "https://elk.bookmeter.com/users/580549/books/read" >
type BookJson = JsonProvider<""" { "title":"羆嵐", "date":[2017, 3, 8], "author":"吉村 昭", "page":226, "comment":"おもしろかった" } """>

let parseDate date =
    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None
    match date with
    | Regex @"([0-9]{4})年([0-9]{2})月([0-9]{2})日" [ year; month; day ] ->
        [|int year; int month; int day|]
    | _ -> [|-1; -1; -1|]

// span class="content__netabareflag"
// review__content review__content--netabare"
let getComment (node : HtmlNode) =
    let url = node.Descendants["a"] 
              |> Seq.map (fun (x:HtmlNode) -> x.Attribute("href").Value())
              |> Seq.filter (fun x -> x.Contains("/reviews/"))
    match Seq.isEmpty url with // 感想があるかどうか
    | true -> ""
    | false -> (let url = Seq.head url in
               let data = HtmlDocument.Load("https://elk.bookmeter.com" + url) in
               let content = data.Descendants("div")
                             |> Seq.filter (fun (x:HtmlNode) -> 
                             x.HasAttribute("class", "review__content review__content--default") || x.HasAttribute("class", "review__content review__content--netabare")) in
               if Seq.isEmpty content then "" 
               else (Seq.head content).InnerText())

// <a href="/books/222470">
// <h1 class="inner__title">
let getTitle (node : HtmlNode) = 
    let url = node.Descendants["a"]
              |> Seq.filter (fun (x:HtmlNode) -> x.Attribute("href").Value().Contains("/books/"))
              |> Seq.head
              |> fun x -> x.Attribute("href").Value()
              |> (+) "https://elk.bookmeter.com"
    let data = HtmlDocument.Load url
    data.Descendants["h1"]
    |> Seq.filter (fun (x:HtmlNode) -> x.HasAttribute("class", "inner__title"))
    |> Seq.head
    |> fun x -> x.InnerText()

let getJson (group : HtmlNode) =
    let node = group.Descendants["div"] 
                   |>  Seq.filter (fun (x:HtmlNode) -> x.HasAttribute("class", "book__detail"))
                   |> Seq.head
    let getDiv key = node.Descendants["div"]
                     |> Seq.filter(fun (x:HtmlNode) -> x.HasAttribute("class", key))
                     |> Seq.head
    let date = getDiv "detail__date" 
               |> (fun x -> x.InnerText())
               |> parseDate
    let title = getDiv "detail__title"
               |> (fun x -> x.InnerText())
               |> (fun x -> if x.Contains("…") then getTitle node else x)
    let author = node.Descendants["li"] 
               |> Seq.head |> (fun x -> x.InnerText())           
    let page = getDiv "detail__page"
               |> (fun x -> x.InnerText() |> int)
    let comment = getComment group           
    BookJson.Root(title, date, author, page, comment).ToString()               

let rec scraper (url : string) old = 
    let data = HtmlDocument.Load url in
    let newList = data.Descendants["li"]  // 20 件所得して処理
                  |>  Seq.filter (fun (x:HtmlNode) -> x.HasAttribute("class", "group__book"))
                  |> Seq.map getJson
                  |> Seq.toList
    let _newUrl = data.Descendants["a"]
                |> Seq.filter (fun (x : HtmlNode) -> x.HasAttribute("rel", "next"))
                |> Seq.map (fun (x : HtmlNode) -> x.Attribute("href").Value())
    match Seq.isEmpty _newUrl with
    | true -> eprintfn "page %d done" ((List.length old) / 20 + 1); old @ newList
    | false -> let newUrl = "https://elk.bookmeter.com" + (Seq.head _newUrl)
               eprintfn "page %d done" ((List.length old) / 20 + 1)
            //    Thread.Sleep(5000)  // sleep
               scraper newUrl (old @ newList)

let pprinter file p =
    let rec aux = function
        | [] -> ()
        | [x] -> fprintfn file "%s" x
        | x::xs -> (fprintfn file "%s," x; aux xs)
    fprintf file "["; aux p; fprintfn file "]"        

[<EntryPoint>]
let main argv =
    eprintf "マイページのURL : "
    let url = Console.ReadLine() + "/books/read"
    eprintf "出力先(json) : "
    let fileName = Console.ReadLine()
    use file = System.IO.File.CreateText fileName
    let bookList = scraper url []
    do bookList |> pprinter file
    0 // return an integer exit code