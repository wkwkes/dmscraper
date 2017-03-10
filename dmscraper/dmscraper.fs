module DmScraper

open FSharp.Data

// let url = "https://elk.bookmeter.com/users/580549/books/read"

type Books = HtmlProvider< "https://elk.bookmeter.com/users/580549/books/read" >
let data = Books.Load("https://elk.bookmeter.com/users/580549/books/read")

let bookList = data.Html.Descendants["div"] 
               |>  Seq.filter (fun (x:HtmlNode) -> x.HasAttribute("class", "book__detail"))
               
// bookList |> Seq.iter (fun x -> x.ToString() |> printfn "--\n%s" )

let 


[<EntryPoint>]
let main argv = 
    // Seq.iter (fun (x, y) -> printfn "%A %A" x y) b 
    0 // return an integer exit code
