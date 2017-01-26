﻿
(*
Much of the below code was heavily inspired by selectorgadget
You can find the source of it here:  https://github.com/cantino/selectorgadget
*)

#r "node_modules/fable-core/Fable.Core.dll"

open Fable.Core
open Fable.Core.JsInterop
open Fable.Import.Browser

//Types
type Self = { Self : obj }
type Element = { Tag : string; Class : string; Id : string; Text : string; Value : string; Name : string; Placeholder : string; Href : string }
type SelectorType = XPath | Css | JQuery | Canopy
type Result = { Selector : string; Readability : float; Count : int; Type : SelectorType; ApplySelector : string}

//Varios JS helpers
let jq = importDefault<obj> "jquery"
let find selector = jq $ selector
let append selector element = (find selector)?append(element) |> ignore
let css selector property value =  (find selector)?css(property, value) |> ignore
let click selector f =  (find selector)?click(f) |> ignore
let value selector = (find selector)?``val``() 
let set selector value = (find selector)?``val``(value) |> ignore
let tag element = (element?prop("tagName") |> string).ToUpper()
let remove selector = (find selector)?remove() |> ignore
let hide selector = (find selector)?hide() |> ignore
let exists selector = 
  let value = (find selector)?("length") |> sprintf "%O" |> int
  value > 0
let off selector event = (find selector)?off(event) |> ignore
let on element event f = 
  let element = jq $ element  
  element?on(event, { Self = element }, f) |> ignore
let onEach selector event f = 
  let elements = find selector
  jq?each(elements, fun index element -> on element event f) |> ignore
let bool whatever = 
  match sprintf "%O" whatever with
  | "true" -> true
  | _      -> false
let px value = sprintf "%ipx" value
let toInt value = value |> sprintf "%O" |> int
let cleanString value =   
  let value = string value
  let value = if value = null then "" else value
  let value = if value = "undefined" then "" else value
  let value = if value.Contains("\n") then "" else value
  let value = if value.Contains("\r\n") then "" else value
  value
let lower (value : string) = value.ToLower()
let hasDigit (value : string) = System.Text.RegularExpressions.Regex.IsMatch(value, @"\d")
let hrefStopAtDigits (href : string) =
  let parts = href.Split('/')
  let index = parts |> Array.tryFindIndex hasDigit
  match index with 
  | Some(index) -> parts |> Array.take index |> fun parts -> System.String.Join ("/", parts)
  | None        -> href
let hrefStopAtQueryString (href : string) =
  if href.Contains("?") then href.Split('?') |> Array.head
  else href
let hrefStopAtHash (href : string) =
  if href.Contains("#") then href.Split('#') |> Array.head
  else href
  
let typeToString type' =
  match type' with
  | XPath -> "xpath"
  | Css -> "css"
  | JQuery-> "jQuery"
  | Canopy -> "canopy"

//Constants
let border_width = 5
let border_padding = 2

let applyXPath selector = 
  let result = document.evaluate(selector, document, null, 0.0, null)
  let mutable element : obj = !!result.iterateNext()
  !! [| while element <> null do yield element; element <- result.iterateNext() |]
  
//Suggestion stuff
let howManyXPath selector = 
  let result = document.evaluate(selector, document, null, 0.0, null)
  [ while result.iterateNext() <> null do yield 1 ] |> List.length

let howManyJQuery selector = !!(find selector)?length |> int

let suggestByXPathText element = 
  if element.Text <> "" then
    let selector = sprintf "//%s[text()='%s']" element.Tag element.Text
    Some {
      Selector = selector
      Count = howManyXPath selector
      Readability = 1.3
      Type = XPath
      ApplySelector = selector
    }
  else None

let suggestByCanopyText element = 
  if element.Text <> "" then
    let selector = sprintf "//*[text()='%s']" element.Text
    Some {
      Selector = element.Text
      Count = howManyXPath selector
      Readability = 0.5
      Type = Canopy
      ApplySelector = selector
    }
  else None

let suggestByName element =
  if element.Name <> "" then
    let selector = sprintf "[name='%s']" element.Name
    Some {
      Selector = selector
      Count = howManyJQuery selector
      Readability = 1.2
      Type = Css
      ApplySelector = selector
    }
  else None

let suggestByPlaceholder element =
  if element.Placeholder <> "" then
    let selector = sprintf "[placeholder='%s']" element.Placeholder
    Some {
      Selector = selector
      Count = howManyJQuery selector
      Readability = 1.2
      Type = Css
      ApplySelector = selector
    }
  else None

let suggestById element = 
  if element.Id <> "" then
    let selector = sprintf "#%s" element.Id
    Some {
      Selector = selector
      Count = howManyJQuery selector
      Readability = 0.3
      Type = Css
      ApplySelector = selector
    }
  else None

let suggestByValue element = 
  if element.Value <> "" then
    let selector = sprintf "[value='%s']" element.Value
    Some {
      Selector = selector
      Count = howManyJQuery selector
      Readability = 1.0
      Type = Css
      ApplySelector = selector
    }
  else None

let suggestByCanopyValue element = 
  if element.Value <> "" then
    let selector = sprintf "//*[@value='%s']" element.Value
    Some {
      Selector = element.Value
      Count = howManyXPath selector
      Readability = 0.5
      Type = Canopy
      ApplySelector = selector
    }
  else None

let suggestByClass element = 
  if element.Class <> "" then
    let classes = sprintf ".%s" (System.String.Join(".", element.Class.Split(' ')))
    let selector = classes
    Some {
      Selector = selector
      Count = howManyJQuery selector
      Readability = 1.5
      Type = Css
      ApplySelector = selector
    }
  else None

let suggestBySingleClass element = 
  if element.Class <> "" then
      element.Class.Split(' ') 
      |> Array.map (fun class' -> sprintf ".%s" class')
      |> Array.map (fun class' ->
        Some {
          Selector = class'
          Count = howManyJQuery class'
          Readability = 1.2
          Type = Css
          ApplySelector = class'
        })
      |> List.ofArray
  else [None]

let suggestByHref element = 
  if element.Href <> "" then    
    let selector = sprintf "[href='%s']" element.Href
    Some {
      Selector = selector
      Count = howManyJQuery selector
      Readability = 1.2
      Type = Css
      ApplySelector = selector
    }
  else None

let suggestByHrefStopAtDigit element = 
  let href = hrefStopAtDigits element.Href
  if element.Href <> "" && href <> element.Href then
    let selector = sprintf "[href^='%s']" href
    Some {
      Selector = selector
      Count = howManyJQuery selector
      Readability = 1.3
      Type = Css
      ApplySelector = selector
    }
  else None

let suggestByHrefStopAtQueryString element = 
  let href = hrefStopAtQueryString element.Href
  if element.Href <> "" && href <> element.Href then    
    let selector = sprintf "[href^='%s']" href
    Some {
      Selector = selector
      Count = howManyJQuery selector
      Readability = 1.3
      Type = Css
      ApplySelector = selector
    }
  else None

let suggestByHrefStopAtHash element = 
  let href = hrefStopAtHash element.Href
  if element.Href <> "" && href <> element.Href then    
    let selector = sprintf "[href^='%s']" href
    Some {
      Selector = selector
      Count = howManyJQuery selector
      Readability = 1.3
      Type = Css
      ApplySelector = selector
    }
  else None

let suggestByTag element = 
  if element.Tag <> "" then    
    let selector = sprintf "%s" element.Tag
    Some {
      Selector = selector
      Count = howManyJQuery selector
      Readability = 1.2
      Type = Css
      ApplySelector = selector
    }
  else None

let suggest element =
  [
    suggestById element
    suggestByName element
    suggestByPlaceholder element
    suggestByCanopyText element
    suggestByXPathText element
    suggestByValue element
    suggestByCanopyValue element
    suggestByClass element
    suggestByHref element    
    suggestByHrefStopAtDigit element
    suggestByHrefStopAtQueryString element
    suggestByHrefStopAtHash element
    suggestByTag element
    //Add parent based stuff    
  ]
  |> List.append (suggestBySingleClass element)
  |> List.choose id
  |> List.filter (fun result -> result.Count > 0)
  |> List.distinctBy (fun result -> result.Selector)
  |> List.map (fun result -> result.Readability * (float result.Selector.Length), result)
  |> List.sortBy ( fun (score, result) -> result.Count, score)
  |> fun results -> if results.Length >= 5 then List.take 5 results else results

//Main Controls
let inputs = """
<div id="canopy_companion" class="canopy_companion_module">
  <input type="button" id="clear" class="canopy_companion_module" value="Clear">
  <input type="button" id="close" class="canopy_companion_module" value="X">
</div>"""

//The borders used around elements
let top =    (jq $ "<div>")?addClass("canopy_companion_border canopy_companion_module")?addClass("canopy_companion_border_top")
let bottom = (jq $ "<div>")?addClass("canopy_companion_border canopy_companion_module")?addClass("canopy_companion_border_bottom")
let left =   (jq $ "<div>")?addClass("canopy_companion_border canopy_companion_module")?addClass("canopy_companion_border_left")
let right =  (jq $ "<div>")?addClass("canopy_companion_border canopy_companion_module")?addClass("canopy_companion_border_right")

//Make new green borders for apply
let green_border heightValue widthvalue topValue leftValue =
  (jq $ "<div>")
    ?addClass("canopy_companion_border")
    ?addClass("canopy_companion_border_green")
    ?addClass("canopy_companion_module")
    ?css("height", px heightValue)
    ?css("width", px widthvalue)
    ?css("top", px topValue)
    ?css("left", px leftValue)
    |> append "body"

//Given elements, draw green borders around it
let createGreenBorders elements = 
  remove ".canopy_companion_border_green"
  jq?each(elements, fun index element ->
    let clone = jq $ element
    let position = clone?offset()
    let top = position?top |> toInt
    let left = position?left  |> toInt
    let width = clone?outerWidth() |> toInt
    let height = clone?outerHeight() |> toInt
        
    green_border border_width (width + border_padding * 2 + border_width * 2) (top - border_width - border_padding) (left - border_padding - border_width)
    green_border border_width (width + border_padding * 2 + border_width * 2 - border_width) (top + height + border_padding) (left - border_padding - border_width)
    green_border (height + border_padding * 2) border_width (top - border_padding) (left - border_padding - border_width)
    green_border (height + border_padding * 2) border_width (top - border_padding) (left + width + border_padding)
  ) |> ignore

//Set the position values for the right provided border
let border position heightValue widthvalue topValue leftValue =
  let element = find (sprintf ".canopy_companion_border_%s" position)
  element
    ?css("height", px heightValue)
    ?css("width", px widthvalue)
    ?css("top", px topValue)
    ?css("left", px leftValue)
    ?show()
    |> ignore

//Given an element, draw borders around it
let createBorders elements = 
  jq?each(elements, fun index element ->
    let clone = jq $ element
    let position = clone?offset()
    let top = position?top |> toInt
    let left = position?left  |> toInt
    let width = clone?outerWidth() |> toInt
    let height = clone?outerHeight() |> toInt
        
    border "top"    border_width (width + border_padding * 2 + border_width * 2) (top - border_width - border_padding) (left - border_padding - border_width)
    border "bottom" border_width (width + border_padding * 2 + border_width * 2 - border_width) (top + height + border_padding) (left - border_padding - border_width)
    border "left"   (height + border_padding * 2) border_width (top - border_padding) (left - border_padding - border_width)
    border "right"  (height + border_padding * 2) border_width (top - border_padding) (left + width + border_padding)
  ) |> ignore

//Result template
let result result' index =   
  (jq $ sprintf 
      """<div>
          selector: <span id="selector_%i" class="canopy_companion_module">"%s"</span> 
          count: %i 
          type: %A 
          <input class="canopy_companion_module" type="button" id="selector_copy_%i" value="Copy"> 
          <input class="canopy_companion_module" type="button" id="selector_apply_%i" value="Apply" data-selector="%s" data-type="%s">
        </div>""" index result'.Selector result'.Count (typeToString result'.Type) index index result'.ApplySelector (typeToString result'.Type))
    ?addClass("canopy_companion_module")
    ?addClass("canopy_companion_result")
    ?css("bottom", px (40 + 33 * index))
  |> append "body"

  //Wire up the copy button
  (find (sprintf "#selector_copy_%i" index))
    ?on("click.selector_copy", (fun _ ->
      let selector = (find (sprintf "#selector_%i" index))?text() |> string
      jq $ "<textarea id='magic_textarea'>" |> append "body"
      (find "#magic_textarea")?``val``(selector) |> ignore
      (find "#magic_textarea")?select() |> ignore
      document.execCommand("copy") |> ignore
      remove "#magic_textarea")) |> ignore

  //Wire up the Apply button
  (find (sprintf "#selector_apply_%i" index))
    ?on("click.selector_apply", (fun _ ->
      let selector = (find (sprintf "#selector_apply_%i" index))?data("selector") |> string
      let type' = (find (sprintf "#selector_apply_%i" index))?data("type") |> string
      let elements = 
        match type' with
        | "css"    | "jQuery" -> find (sprintf "%s:not(.canopy_companion_module)" selector)
        | "canopy" | "xpath"  -> applyXPath selector
        | _ -> null
      createGreenBorders elements)) |> ignore
  
//Highlight an element on mouseEnter
let mouseEnter event = 
  let element = jq $ event?data?Self
  let tag = tag element
  if tag <> "BODY" && tag <> "HTML" then
    event?stopImmediatePropagation() |> ignore
    hide ".canopy_companion_border"
    createBorders element
    
//Put a hidden div over an area on click to block the real target being click (to prevent links working etc)
//Fade it out after 400ms
let blockClick element =
  let clone = jq $ element
  let position = clone?offset()
  let top = position?top |> toInt
  let left = position?left  |> toInt
  let width = clone?outerWidth() |> toInt
  let height = clone?outerHeight() |> toInt

  let block = 
    (jq $ "<div>")
      ?css("position", "absolute")
      ?css("z-index",  "9999999")
      ?css("width",  px width)
      ?css("height", px height)
      ?css("top",    px top)
      ?css("left",   px left)
      ?css("background-color", "")

  append "body" block
  window.setTimeout((fun _ -> block?remove()), 400.) |> ignore

//On mouse down, block the real click, grab the element being clicked, and calculate some selectors for it
let mouseDown event = 
  let self = !!event?data?Self?get(0)
  let element = jq $ self
  let tag = tag element
  if tag <> "BODY" && tag <> "HTML" then
    event?stopImmediatePropagation() |> ignore
    blockClick element
    let element =
      {
        Tag =         cleanString <| (lower <| !!self?tagName)
        Class =       cleanString <| !!self?className
        Id =          cleanString <| !!self?id
        Text =        cleanString <| if !!self?textContext = null then !!self?innerText else !!self?textContext
        Value =       cleanString <| !!self?value
        Name =        cleanString <| !!self?name
        Placeholder = cleanString <| !!self?placeholder
        Href =        cleanString <| !!element?attr("href")
      }

    remove ".canopy_companion_result"
    off "*" "click.selector_copy"
    off "*" "click.selector_apply"

    suggest element
    |> List.rev
    |> List.iteri (fun index (_, result') -> result result' index)

//Startup code
//Add the main search box if not there, and wire it up
if not (exists "#canopy_companion") then
  append "body" top
  append "body" bottom
  append "body" left
  append "body" right

  onEach "*:not(.canopy_companion_module)" "mouseenter.canopy" mouseEnter
  onEach "*:not(.canopy_companion_module)" "mousedown.canopy" mouseDown
  
  append "body" inputs

  click "#canopy_companion #clear" (fun _ ->
    remove ".canopy_companion_border_green"
    remove ".canopy_companion_result"
    hide   ".canopy_companion_border")

  click "#canopy_companion #close" (fun _ -> 
    off "*:not(.canopy_companion_module)" "mouseenter.canopy"
    off "*:not(.canopy_companion_module)" "mousedown.canopy"
    remove ".canopy_companion_border"
    remove ".canopy_companion_result"
    remove "#canopy_companion")

//(*

//let mutable tests : (string * (unit -> unit)) list = []
//let ( &&& ) desc f = tests <- List.append tests [desc, f]
//let run () = tests |> List.map (fun (desc, f) -> (printfn "%s" desc; f ()))
//let (==) value1 value2 = System.Console.WriteLine("{0} expected: {1} got: {2}", (value1 = value2), value2, value1)

//*)