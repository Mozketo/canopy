module file2

open canopy
open canopy.core.Assert.Operators
open runner

let testpage = "http://lefthandedgoat.github.io/canopy/testpages/"

let all() =
    context "file2"

    "from file 2 1" &&&&& fun _ ->
        url testpage
        "#welcome" == "Welcome"

    "from file 2 2" &&&&& fun _ ->
        url testpage
        "#welcome" == "Welcome"
