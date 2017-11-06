module DmScraper

open FSharp.Data
open System
open System.IO
open System.Threading
open System.Text.RegularExpressions

type BookJson = JsonProvider<""" { "title":"羆嵐", "date":[2017, 3, 8], "author":"吉村 昭", "page":226, "comment":"良かった" } """>

let parseDate date =
    let (|Regex|_|) pattern input =
        let m = Regex.Match(input, pattern)
        if m.Success then Some(List.tail [ for g in m.Groups -> g.Value ])
        else None
    match date with
    | Regex @"([0-9]{4})/([0-9]{2})/([0-9]{2})" [ year; month; day ] ->
        [|int year; int month; int day|]
    | _ -> [|-1; -1; -1|]

let editComment s = 
//   printf "s: %A" s; printfn " id: %A" id
  let re = ".*content_tag\":\"([^\"]*)\",\"(.*)"
  let (|Regex|_|) pattern input =
    let m = Regex.Match(input, pattern)
    if m.Success then Some (List.item 1 [ for g in m.Groups -> g.Value ])
    else None
  match s with 
  | Regex re ss -> ss
  | _ -> "regexp fail"

let getComment (node : HtmlNode) =
    let url = node.Descendants["a"] 
              |> Seq.map (fun (x:HtmlNode) -> x.Attribute("href").Value())
              |> Seq.filter (fun x -> x.Contains("/reviews/") && x.Contains("/users/"))
    match Seq.isEmpty url with // 感想があるかどうか
    | true -> ""
    | false -> (let url = Seq.head url in
               let data = HtmlDocument.Load("https://elk.bookmeter.com" + url) in
               let content = data.Descendants["div"]
                             |> Seq.filter (fun (x:HtmlNode) -> 
                             x.HasAttribute("data-redirect-path-after-delete", "/reviews")) in
               if Seq.isEmpty content then "" 
               else editComment ((Seq.head content).Attribute("data-resource").Value()) (*url.[9..]*) )

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

let getJson (node : HtmlNode) = 
    let getText a1 a2 key = node.Descendants[a1]
                            |> Seq.filter(fun (x:HtmlNode) -> x.HasAttribute(a2, key))
                            |> Seq.head
                            |> fun x -> x.InnerText()
    let date = getText "div" "class" "detail__date" |> parseDate
    let title = getText "div" "class" "detail__title"
               |> (fun x -> if x.Contains("…") then getTitle node else x)
    let author = getText "ul" "class" "detail__authors"
    let page = getText "div" "class" "detail__page" |> int
    let comment = getComment node
    BookJson.Root(title, date, author, page, comment).ToString()    

let rec scraper (url : string) old = 
    let data = HtmlDocument.Load url in
    let newList = data.Descendants["li"]  // 20 件所得して処理
                  |> Seq.filter (fun (x:HtmlNode) -> x.HasAttribute("class", "group__book"))
                  |> Seq.map getJson
                  |> Seq.toList
    let _newUrl = data.Descendants["a"]
                |> Seq.filter (fun (x : HtmlNode) -> x.HasAttribute("rel", "next"))
                |> Seq.map (fun (x : HtmlNode) -> x.Attribute("href").Value())
    match Seq.isEmpty _newUrl with // 最後のページか
    | true -> eprintfn "page %d done" ((List.length old) / 20 + 1); old @ newList
    | false -> let newUrl = "https://elk.bookmeter.com" + (Seq.head _newUrl)
               eprintfn "page %d done" ((List.length old) / 20 + 1)
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

    