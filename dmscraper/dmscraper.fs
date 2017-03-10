module DmScraper

open FSharp.Data
open System.Text.RegularExpressions

type BookHtml = HtmlProvider< "https://elk.bookmeter.com/users/580549/books/read" >
type BookJson = JsonProvider<""" { "date":[2017, 3, 8], "title":"羆嵐", "author":"吉村 昭", "page":226 } """>

let parseDate date =
    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None
    match date with
    | Regex @"([0-9]{4})年([0-9]{2})月([0-9]{2})日" [ area; prefix; suffix ] ->
        [|int area; int prefix; int suffix|]
    | _ -> [|-1; -1; -1|]

let getJson (node : HtmlNode) =
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
    BookJson.Root(date, title, author, page)                        

[<EntryPoint>]
let main argv =
    let data = BookHtml.Load("https://elk.bookmeter.com/users/580549/books/read") 
    let bookList = data.Html.Descendants["div"] |>  Seq.filter (fun (x:HtmlNode) -> x.HasAttribute("class", "book__detail"))
    do bookList |> Seq.iter (getJson >> printfn "%A")
    0 // return an integer exit code
