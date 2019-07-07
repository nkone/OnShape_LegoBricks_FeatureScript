# **************************************************************************** #
#                                                                              #
#                                                         :::      ::::::::    #
#    brick.fs                                           :+:      :+:    :+:    #
#                                                     +:+ +:+         +:+      #
#    By: phtruong <marvin@42.fr>                    +#+  +:+       +#+         #
#                                                 +#+#+#+#+#+   +#+            #
#    Created: 2019/07/07 13:59:02 by phtruong          #+#    #+#              #
#    Updated: 2019/07/07 13:59:26 by phtruong         ###   ########.fr        #
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
        initializeVar(context, definition);
        baseSketch(context, id);
        extrudeBase(context, id);
        studSketch(context, id);
        extrudeStud(context, id);
        shellBase(context, id);
        if (definition.text)
        {
            setVariable(context, "studtext", "Lego");
            textSketch(context, id);
            textExtrude(context, id);
        }
        if ((definition.col == 1 && definition.row > 1) || (definition.col > 1 && definition.row == 1)) {
             solidInnerCol(context, id);
             unifySolid(context, id);
             deleteBodies(context, id + "solid_delete", { "entities" : qUnion([qCreatedBy(id + "solid", EntityType.BODY)])});
        }
        if (definition.col > 1 && definition.row > 1) {
            innerHollow(context, id);
            unifySolid(context, id);
            deleteBodies(context, id + "hollow_delete", { "entities" : qUnion([qCreatedBy(id + "hollow_inner", EntityType.BODY)])});
        }
        //Clear ketches
        deleteBodies(context, id + "stud_delete", { "entities" : qUnion([qCreatedBy(id + "stud", EntityType.BODY),
                                                                        qCreatedBy(id + "base", EntityType.BODY),
                                                                        qCreatedBy(id + "stud_text", EntityType.BODY)])});
    });
    /*
    ** function initializeVar:
    ** Set initialize variables
    ** Default unit: millimeter
    ** Functionality: create global variables
    */
    function initializeVar(context is Context, definition is map)
    {
        setVariable(context, "width", 8);
        setVariable(context, "base_height", 9.6);
        setVariable(context, "stud_height", 1.6);
        setVariable(context, "stud_dia", 4.8);
        setVariable(context, "thic", 1.6);
        setVariable(context, "solid_inner_dia", 3.2);
        setVariable(context, "hollow_outer_dia", 6.4);
        setVariable(context, "hollow_inner_dia", 4.8);
        setVariable(context, "col", definition.col);
        setVariable(context, "row", definition.row);
    }
    /*
    ** function baseSketch:
    ** Sketch the base for extrusion
    ** Default unit: millimeter
    ** Functionality: Sketches a 1 by 1 lego piece
    */
    function baseSketch(context is Context, id is Id)
    {
        var sketch_base = newSketch(context, id + "base", {
                "sketchPlane" : qCreatedBy(makeId("Top"), EntityType.FACE)
        });
        var width = getVariable(context, "width");
        var col = getVariable(context, "col");
        var row = getVariable(context, "row");
        skRectangle(sketch_base, "base", {
                "firstCorner" : vector(0, 0) * millimeter,
                "secondCorner" : vector(width * row, width * col) * millimeter
        });
        skSolve(sketch_base); 
    }
    /*
    ** function extrudeBase:
    ** Using the previous sketch, extrude a 1 by 1 lego piece
    ** Default unit: millimeter
    ** Functionality: Gets height from global and extrude blind base on previous sketch
    */
    function extrudeBase(context is Context, id is Id)
    {
        var height = getVariable(context, "base_height");
        opExtrude(context, id + "base_extrude", {
                "entities" : qSketchRegion(id + "base"),
                "direction" : evOwnerSketchPlane(context, {"entity" : qSketchRegion(id + "base")}).normal,
                "endBound" : BoundingType.BLIND,
                "endDepth" : height * millimeter
        });
    }
    /*
    ** function studSketch:
    ** Sketches the studs on top of the lego brick
    ** Default unit: millimeter
    ** Functionality: loops through row and columns and sketch the studs on top of the base extrude
    */
    function studSketch(context is Context, id is Id)
    {
        var sketch_stud = newSketch(context, id + "stud", {
                "sketchPlane" : qNthElement(qCreatedBy(id + "base_extrude", EntityType.FACE), 2)
        });
        
        var stud_radius = getVariable(context, "stud_dia")/2;
        var stud_id = 0;
        var width = getVariable(context, "width");
        var col = getVariable(context, "col");
        var row = getVariable(context, "row");
        var stud_center_x;
        var stud_center_y;
        for (var c = 0; c < col; c += 1)
        {
            for (var r = 0; r < row; r += 1)
            {
                stud_center_x = (width/2 + width * r);
                stud_center_y = (width/2 + width * c);
                skCircle(sketch_stud, "stud" ~ stud_id, {
                    "center" : vector(stud_center_x, stud_center_y) * millimeter,
                    "radius" : stud_radius * millimeter
                });
                stud_id += 1;
            }
        }
        skSolve(sketch_stud);
    }
    /*
    ** function extrudeStud:
    ** Extrude the studs base on previous sketch
    ** Default units: millimeter
    ** Functionality: extrude the studs on the surface of the base extrude
    */
    function extrudeStud(context is Context, id is Id)
    {
        var stud_height = getVariable(context, "stud_height");
        extrude(context, id + "stud_extrude", {
                        "entities" : qSketchRegion(id + "stud", true),
                        "endBound" : BoundingType.BLIND,
                        "operationType" : NewBodyOperationType.ADD,
                        "depth" : stud_height * millimeter,
                        "defaultScope" : false,
                        "booleanScope": qUnion([qCreatedBy(id + "base_extrude", EntityType.BODY)])
                    });
    }
    /*
    ** function textSketch:
    ** Sketches the text on top of the studs
    ** Default unit: millimeter
    ** Functionality: Sketches the text using the two corners, offset from the center of the studs
    */
    function textSketch(context is Context, id is Id)
    {
        var col = getVariable(context, "col");
        var row = getVariable(context, "row");
        var width = getVariable(context, "width");
        var text_id = 0;
        var stud_center_x;
        var stud_center_y;
        var first_corner_x;
        var first_corner_y;
        var second_corner_x; 
        var second_corner_y;
        var stud_text is string = getVariable(context, "studtext");
        var studText = newSketch(context, id + "stud_text", {
                      "sketchPlane" : qNthElement(qCreatedBy(id + "stud_extrude", EntityType.FACE), 1)
                      });
        var text_name = "text";
        for (var c = 0; c < col; c += 1)
        {
            for (var r = 0; r < row; r += 1)
            {
                stud_center_x = (width/2 + width * r);
                stud_center_y = (width/2 + width * c);
                first_corner_x = (stud_center_x - 2);
                first_corner_y = (stud_center_y - 0.703);
                second_corner_x = (stud_center_x + 0.5);
                second_corner_y = (stud_center_y + 0.703);
                skText(studText, text_name ~ text_id, {
                        "fontName" : "OpenSans-Italic.ttf",
                        "text" : stud_text,
                        "firstCorner" : vector(first_corner_x,first_corner_y) * millimeter,
                        "secondCorner" : vector(second_corner_x, second_corner_y) * millimeter});
                text_id += 1;
            }
        }
        skSolve(studText);
    }
    /*
    ** function textExtrude:
    ** Extrude the text region from the previous sketch
    ** Default unit: millimeter
    ** Functionality: Extrude the text on the studs
    */
    function textExtrude(context is Context, id is Id)
    {
        // Extrude just text region
        extrude(context, id + "textExtrude", {
                            "entities" : qSketchRegion(id + "stud_text", true),
                            "endBound" : BoundingType.BLIND,
                            "operationType" : NewBodyOperationType.ADD,
                            "depth" : 0.1 * millimeter,
                            "defaultScope" : false,
                            "booleanScope": qUnion([qCreatedBy(id + "base_extrude", EntityType.BODY)])
        });
    }
    /*
    ** function shellBase:
    ** Shell the bricks
    ** Default unit: millimeter
    ** Functionality: Creates a hollow block of brick
    */
    function shellBase(context is Context, id is Id)
    {
        // Shell inward with negative thickness.
        var thic = getVariable(context, "thic");
        opShell(context, id + "shell_base", {
                "isHollow" : false,
                "entities" : qNthElement(qCreatedBy(id + "base_extrude", EntityType.FACE), 1),
                "thickness" : -thic * millimeter
        });
    }
    /*
    ** function solidInnerCol:
    ** Creates solid inner columns support
    ** Default unit: millimeter
    ** Functionality: For blocks consists of 1 by x or x by 1, create solid tubes
    */
    function solidInnerCol(context is Context, id is Id)
    {
        var stud_dia = getVariable(context, "stud_dia");
        var col_r = getVariable(context, "solid_inner_dia")/2;
        var solid_inner = newSketch(context, id + "solid", {
                "sketchPlane" : qNthElement(qCreatedBy(id + "base_extrude", EntityType.FACE), 5)
        });
        var mid_pt_y = getVariable(context, "width")/2;
        var mid_pt_x = getVariable(context, "stud_dia") + col_r * 2;
        var col = getVariable(context, "col");
        var row = getVariable(context, "row");
        var col_id = 0;
        var count = (row > col) ? row : col;
        mid_pt_y = (row > col) ? -mid_pt_y : -mid_pt_x;
        mid_pt_x = (row > col) ? mid_pt_x : getVariable(context, "width")/2;
        for (var c = 0; c < count - 1; c += 1) {
            skCircle(solid_inner, "solid" ~ col_id, {
                    "center" : vector(mid_pt_x, mid_pt_y) * millimeter,
                    "radius" : col_r * millimeter
            });
            
            mid_pt_x += (row > col) ? stud_dia + col_r * 2 : 0;
            mid_pt_y += (row > col) ? 0 : -(stud_dia + col_r * 2);
            col_id += 1;
        }
        skSolve(solid_inner);
        opExtrude(context, id + "inner_ex", {
                "entities" : qSketchRegion(id + "solid"),
                "direction" : evOwnerSketchPlane(context, {"entity" : qSketchRegion(id + "solid")}).normal * -1,
                "endBound" : BoundingType.BLIND,
                "operationType" : NewBodyOperationType.ADD,
                "endDepth" : 9.6 * millimeter,
                "defaultScope" : false,
                "booleanScope": qUnion([qCreatedBy(id + "base_extrude", EntityType.BODY)])
        });
    }
    /*
    ** function innerHollow:
    ** Creates hollow columns support
    ** Default unit: millimeter
    ** Functionality: For blocks that have more than 2 for rows and columns, create hollow tubes
    */
    function innerHollow(context is Context, id is Id)
    {
        var hollow_inner = newSketch(context, id + "hollow_inner", {
                "sketchPlane" : qNthElement(qCreatedBy(id + "base_extrude", EntityType.FACE), 5)
        });
        var hol_big_dia = getVariable(context, "hollow_outer_dia");
        var hol_small_dia = getVariable(context, "hollow_inner_dia");
        var hol_mid_x = getVariable(context, "width");
        var hol_mid_y = -hol_mid_x;
        var hol_id = 0;
        var row = getVariable(context, "row");
        var col = getVariable(context, "col");
        for (var c = 2; c <= col; c += 1)
        {
            hol_mid_x = getVariable(context, "width");
            for (var r = 2; r <= row; r += 1)
            {
                skCircle(hollow_inner, "hollow" ~ hol_id, {
                    "center" : vector(hol_mid_x, hol_mid_y) * millimeter,
                    "radius" : hol_big_dia/2 * millimeter
                    });
                skCircle(hollow_inner, "hollow_small" ~ hol_id, {
                    "center" : vector(hol_mid_x, hol_mid_y) * millimeter,
                    "radius" : hol_small_dia/2 * millimeter
                    });
                hol_mid_x += getVariable(context, "width");
                hol_id += 1;
            }
            hol_mid_y -= getVariable(context, "width");
        }
        skSolve(hollow_inner);
        opExtrude(context, id + "hollow_ex", {
                "entities" : qSketchRegion(id + "hollow_inner", true),
                "direction" : evOwnerSketchPlane(context, {"entity" : qSketchRegion(id + "hollow_inner")}).normal * -1,
                "endBound" : BoundingType.BLIND,
                "operationType" : NewBodyOperationType.ADD,
                "endDepth" : 9.6 * millimeter,
                "defaultScope" : false,
                "booleanScope": qUnion([qCreatedBy(id + "base_extrude", EntityType.BODY)])
        });
        
    }
    /*
    ** function unifySolid:
    ** Unifies all extrude solids
    */
    function unifySolid(context is Context, id is Id)
    {
        opBoolean(context, id + "union_all", {
                "tools" : qAllNonMeshSolidBodies(),
                "operationType" : BooleanOperationType.UNION
        });
    }
