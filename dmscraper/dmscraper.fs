module DmScraper

open FSharp.Data
open System.Text.RegularExpressions

// type BookHtml = HtmlProvider< "https://elk.bookmeter.com/users/580549/books/read" >
type BookJson = JsonProvider<""" { "date":[2017, 3, 8], "title":"羆嵐", "author":"吉村 昭", "page":226, "comment":"おもしろかった" } """>

let parseDate date =
    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None
    match date with
    | Regex @"([0-9]{4})年([0-9]{2})月([0-9]{2})日" [ year; month; day ] ->
        [|int year; int month; int day|]
    | _ -> [|-1; -1; -1|]

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
                             x.HasAttribute("class", "review__content review__content--default")) in
               if Seq.isEmpty content then "" 
               else (Seq.head content).InnerText())

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
    let author = node.Descendants["li"] 
               |> Seq.head |> (fun x -> x.InnerText())           
    let page = getDiv "detail__page"
               |> (fun x -> x.InnerText() |> int)
    let comment = getComment group           
    BookJson.Root(date, title, author, page, comment)                        

[<EntryPoint>]
let main argv =
    let data = HtmlDocument.Load("https://elk.bookmeter.com/users/580549/books/read")
    let bookList = data.Descendants["li"] 
                   |>  Seq.filter (fun (x:HtmlNode) -> x.HasAttribute("class", "group__book"))
    do bookList |> Seq.iter (getJson >> (printfn "%A,"))
    0 // return an integer exit code