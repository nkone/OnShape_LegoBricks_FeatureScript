# **************************************************************************** #
#                                                                              #
#                                                         :::      ::::::::    #
#    brick.fs                                           :+:      :+:    :+:    #
#                                                     +:+ +:+         +:+      #
#    By: phtruong <marvin@42.fr>                    +#+  +:+       +#+         #
#                                                 +#+#+#+#+#+   +#+            #
#    Created: 2019/07/07 17:28:34 by phtruong          #+#    #+#              #
#    Updated: 2019/07/07 17:28:36 by phtruong         ###   ########.fr        #
#                                                                              #
# **************************************************************************** #

FeatureScript 1096;
import(path : "onshape/std/geometry.fs", version : "1096.0");

annotation { "Feature Type Name" : "Lego Generator" }
export const Lego = defineFeature(function(context is Context, id is Id, definition is map)
    precondition
    {
        // Define the parameters of the feature type
        annotation { "Name" : "Row" }
        isInteger(definition.row, POSITIVE_COUNT_BOUNDS);
        annotation { "Name" : "Col" }
        isInteger(definition.col, POSITIVE_COUNT_BOUNDS);
        annotation { "Name" : "Text" }
        definition.text is boolean;
        
    }
    {
        // Define the function's action
        var width = 8;
        var height = 9.6;
        var studDia = 4.8;
        var studHeight = 1.6;
        var thic = 1.6;
        var solDia = 3.2;
        var holOutDia = 6.4;
        var holInDia = 4.8;
        baseSketch(context, id, width, definition.col, definition.row);
        extrudeBase(context, id, height);
        studSketch(context, id, width, definition.col, definition.row, studDia);
        extrudeStud(context, id, studHeight);
        shellBase(context, id, thic);
        if (definition.text)
        {
            setVariable(context, "studtext", "LEGO");
            textSketch(context, id, width, definition.col, definition.row);
            textExtrude(context, id);
        }
        if ((definition.col == 1 && definition.row > 1) || (definition.col > 1 && definition.row == 1)) {
             solidInnerCol(context, id, width, studDia, solDia, definition.col, definition.row);
             unifySolid(context, id);
             deleteBodies(context, id + "solidDelete", { "entities" : qUnion([qCreatedBy(id + "solid", EntityType.BODY)])});
        }
        if (definition.col > 1 && definition.row > 1) {
            innerHollow(context, id, width, holOutDia, holInDia, definition.col, definition.row);
            unifySolid(context, id);
            deleteBodies(context, id + "hollowDelete", { "entities" : qUnion([qCreatedBy(id + "hollowInner", EntityType.BODY)])});
        }
        //Clear sketches
        deleteBodies(context, id + "studDelete", { "entities" : qUnion([qCreatedBy(id + "stud", EntityType.BODY),
                                                                        qCreatedBy(id + "base", EntityType.BODY),
                                                                        qCreatedBy(id + "studText", EntityType.BODY)])});
    });
    /*
    ** function baseSketch:
    ** Sketch the base for extrusion
    ** Parameters:
    ** [context] is Context (built in data structure)
    ** [id] is Id (built in data structure)
    ** [width] is width of the 1 by 1 lego piece
    ** [col] is number of columns
    ** [row] is number of rows
    ** Default unit: millimeter
    ** Functionality: Sketches the base of the brick
    ** Return: NULL
    */
    function baseSketch(context is Context, id is Id, width is number, col is number, row is number)
    {
        var sketchBase = newSketch(context, id + "base", {
                "sketchPlane" : qCreatedBy(makeId("Top"), EntityType.FACE)
        });
        skRectangle(sketchBase, "base", {
                "firstCorner" : vector(0, 0) * millimeter,
                "secondCorner" : vector(width * row, width * col) * millimeter
        });
        skSolve(sketchBase); 
    }
    /*
    ** function extrudeBase:
    ** Using the previous sketch, extrude the base with given height
    ** Parameters:
    ** [context] is Context (built in data struture)
    ** [id] is Id (built in data structure)
    ** [height] is the height of the brick
    ** Default unit: millimeter
    ** Functionality: Creates the base of the brick
    ** Return: NULL
    */
    function extrudeBase(context is Context, id is Id, height is number)
    {
        opExtrude(context, id + "baseExtrude", {
                "entities" : qSketchRegion(id + "base"),
                "direction" : evOwnerSketchPlane(context, {"entity" : qSketchRegion(id + "base")}).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : height * millimeter
        });
    }
    /*
    ** function studSketch:
    ** Sketches the studs on top of the lego brick
    ** Parameters:
    ** [context] is Context (built in data struture)
    ** [id] is Id (built in data structure)
    ** [width] is width of the 1 by 1 lego piece
    ** [col] is number of columns
    ** [row] is number of rows
    ** [studDia] is diamater of a stud
    ** Default unit: millimeter
    ** Functionality: loops through row and columns and sketch the studs on top of the base extrude
    ** Return: NULL
    */
    function studSketch(context is Context, id is Id, width is number, col is number, row is number, studDia is number)
    {
        var sketchStud = newSketch(context, id + "stud", {
                "sketchPlane" : qNthElement(qCreatedBy(id + "baseExtrude", EntityType.FACE), 2)
        });
        
        var studRadius = studDia/2;
        var studId = 0;
        var studCenterX;
        var studCenterY;
        for (var c = 0; c < col; c += 1)
        {
            for (var r = 0; r < row; r += 1)
            {
                studCenterX = (width/2 + width * r);
                studCenterY = (width/2 + width * c);
                skCircle(sketchStud, "stud" ~ studId, {
                    "center" : vector(studCenterX, studCenterY) * millimeter,
                    "radius" : studRadius * millimeter
                });
                studId += 1;
            }
        }
        skSolve(sketchStud);
    }
    /*
    ** function extrudeStud:
    ** Extrude the studs base on previous sketch
    ** Parameters:
    ** [context] is Context (built in data struture)
    ** [id] is Id (built in data structure)
    ** [studHeight] is the height of the stud
    ** Default units: millimeter
    ** Functionality: extrude the studs on the surface of the base extrude
    ** Return: NULL
    */
    function extrudeStud(context is Context, id is Id, studHeight is number)
    {
        extrude(context, id + "studExtrude", {
                        "entities" : qSketchRegion(id + "stud", true),
                        "endBound" : BoundingType.BLIND,
                        "operationType" : NewBodyOperationType.ADD,
                        "depth" : studHeight * millimeter,
                        "defaultScope" : false,
                        "booleanScope": qUnion([qCreatedBy(id + "baseExtrude", EntityType.BODY)])
                    });
    }
    /*
    ** function textSketch:
    ** Sketches the text on top of the studs
    ** Parameters:
    ** [context] is Context (built in data struture)
    ** [id] is Id (built in data structure)
    ** [width] is width of a 1 by 1 lego
    ** [col] is number of columns
    ** [row] is number of rows
    ** Default unit: millimeter
    ** Functionality: Sketches the text using the two corners, offset from the center of the studs
    ** Return: NULL
    */
    function textSketch(context is Context, id is Id, width is number, col is number, row is number)
    {
        var textID = 0;
        var studCenterX;
        var studCenterY;
        var firstCornerX;
        var firstCornerY;
        var secondCornerX; 
        var secondCornerY;
        var stud_text is string = "LEGO";
        var studText = newSketch(context, id + "studText", {
                      "sketchPlane" : qNthElement(qCreatedBy(id + "studExtrude", EntityType.FACE), 1)
                      });
        var text_name = "text";
        for (var c = 0; c < col; c += 1)
        {
            for (var r = 0; r < row; r += 1)
            {
                // Find the vector for the center of the stud
                studCenterX = (width/2 + width * r);
                studCenterY = (width/2 + width * c);
                // Find the vectors to create a box to write text on using manual offset
                firstCornerX = (studCenterX - 2);
                firstCornerY = (studCenterY - 0.603);
                secondCornerX = (studCenterX + 0.5);
                secondCornerY = (studCenterY + 0.603);
                skText(studText, text_name ~ textID, {
                        "fontName" : "OpenSans-Italic.ttf",
                        "text" : stud_text,
                        "firstCorner" : vector(firstCornerX,firstCornerY) * millimeter,
                        "secondCorner" : vector(secondCornerX, secondCornerY) * millimeter});
                textID += 1;
            }
        }
        skSolve(studText);
    }
    /*
    ** function textExtrude:
    ** Extrude the text region from the previous sketch
    ** Parameters:
    ** [context] is Context (built in data struture)
    ** [id] is Id (built in data structure)
    ** Default unit: millimeter
    ** Functionality: Extrude the text on the studs
    ** Returns: NULL
    */
    function textExtrude(context is Context, id is Id)
    {
        var textHeight = 0.1;
        // Extrude just text region
        extrude(context, id + "textExtrude", {
                            "entities" : qSketchRegion(id + "studText", true),
                            "endBound" : BoundingType.BLIND,
                            "operationType" : NewBodyOperationType.ADD,
                            "depth" : textHeight * millimeter,
                            "defaultScope" : false,
                            "booleanScope": qUnion([qCreatedBy(id + "baseExtrude", EntityType.BODY)])
        });
    }
    /*
    ** function shellBase:
    ** Shell the bricks
    ** Parameters:
    ** [context] is Context (built in data struture)
    ** [id] is Id (built in data structure)
    ** [thic] is the thickness of the brick
    ** Default unit: millimeter
    ** Functionality: Creates a hollow block of brick
    ** Return: NULL
    */
    function shellBase(context is Context, id is Id, thic is number)
    {
        // Shell inward with negative thickness.
        opShell(context, id + "shellBase", {
                "isHollow" : false,
                "entities" : qNthElement(qCreatedBy(id + "baseExtrude", EntityType.FACE), 1),
                "thickness" : -thic * millimeter
        });
    }
    /*
    ** function solidInnerCol:
    ** Creates solid inner columns support
    ** Paramters:
    ** [context] is Context (built in data struture)
    ** [id] is Id (built in data structure)
    ** [width] is base length of 1 by 1 brick
    ** [studDia] is diameter of a stud
    ** [solDia] is diamater of solid column
    ** [col] is number of columns
    ** [row] is number of rows
    ** Default unit: millimeter
    ** Functionality: For blocks consists of 1 by x or x by 1, create solid tubes
    ** Return: NULL
    */
    function solidInnerCol(context is Context, id is Id, width is number, studDia is number, solDia is number, col is number, row is number)
    {
        var colR = solDia/2;
        var solidInner = newSketch(context, id + "solid", {
                "sketchPlane" : qNthElement(qCreatedBy(id + "baseExtrude", EntityType.FACE), 5)
        });
        var midPtY = width/2;
        var midPtX = studDia + colR * 2;
        var col_id = 0;
        var count = (row > col) ? row : col;
        midPtY = (row > col) ? -midPtY : -midPtX;
        midPtX = (row > col) ? midPtX : width/2;
        for (var c = 0; c < count - 1; c += 1) {
            skCircle(solidInner, "solid" ~ col_id, {
                    "center" : vector(midPtX, midPtY) * millimeter,
                    "radius" : colR * millimeter
            });
            
            midPtX += (row > col) ? studDia + colR * 2 : 0;
            midPtY += (row > col) ? 0 : -(studDia + colR * 2);
            col_id += 1;
        }
        skSolve(solidInner);
        opExtrude(context, id + "innerEx", {
                "entities" : qSketchRegion(id + "solid"),
                "direction" : evOwnerSketchPlane(context, {"entity" : qSketchRegion(id + "solid")}).normal * -1,
                "endBound" : BoundingType.BLIND,
                "operationType" : NewBodyOperationType.ADD,
                "endDepth" : 9.6 * millimeter,
                "defaultScope" : false,
                "booleanScope": qUnion([qCreatedBy(id + "baseExtrude", EntityType.BODY)])
        });
    }
    /*
    ** function innerHollow:
    ** Creates hollow columns support
    ** Paramters:
    ** [context] is Context (built in data struture)
    ** [id] is Id (built in data structure)
    ** [width] is base length of 1 by 1 brick
    ** [holOutDia] is diamater of outer hollow column
    ** [holInDia] is diamater of inner hollow column
    ** [col] is number of columns
    ** [row] is number of rows
    ** Default unit: millimeter
    ** Functionality: For blocks that have more than 2 for rows and columns, create hollow tubes
    ** Return: NULL
    */
    function innerHollow(context is Context, id is Id, width is number, holOutDia is number, holInDia is number, col is number, row is number)
    {
        var hollowInner = newSketch(context, id + "hollowInner", {
                "sketchPlane" : qNthElement(qCreatedBy(id + "baseExtrude", EntityType.FACE), 5)
        });
        var holBigDia = holOutDia;
        var holSmallDia = holInDia;
        var holMidX = width;
        var holMidY = -holMidX;
        var holId = 0;
        for (var c = 2; c <= col; c += 1)
        {
            holMidX = width;
            for (var r = 2; r <= row; r += 1)
            {
                skCircle(hollowInner, "hollow" ~ holId, {
                    "center" : vector(holMidX, holMidY) * millimeter,
                    "radius" : holBigDia/2 * millimeter
                    });
                skCircle(hollowInner, "hollowSmall" ~ holId, {
                    "center" : vector(holMidX, holMidY) * millimeter,
                    "radius" : holSmallDia/2 * millimeter
                    });
                holMidX += width;
                holId += 1;
            }
            holMidY -= width;
        }
        skSolve(hollowInner);
        opExtrude(context, id + "hollowEx", {
                "entities" : qSketchRegion(id + "hollowInner", true),
                "direction" : evOwnerSketchPlane(context, {"entity" : qSketchRegion(id + "hollowInner")}).normal * -1,
                "endBound" : BoundingType.BLIND,
                "operationType" : NewBodyOperationType.ADD,
                "endDepth" : 9.6 * millimeter,
                "defaultScope" : false,
                "booleanScope": qUnion([qCreatedBy(id + "baseExtrude", EntityType.BODY)])
        });
        
    }
    /*
    ** function unifySolid:
    ** Paramters:
    ** [context] is Context (built in data struture)
    ** [id] is Id (built in data structure)
    ** Functionality: Unifies all extrude solids
    ** Return: NULL
    */
    function unifySolid(context is Context, id is Id)
    {
        opBoolean(context, id + "unionAll", {
                "tools" : qAllNonMeshSolidBodies(),
                "operationType" : BooleanOperationType.UNION
        });
    }
