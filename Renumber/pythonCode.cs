using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Renumber
// ReNumber numbered elements in order of selection.
{    
    public static class Module
    {

        public static object logger = script.get_logger();

        public static object output = script.get_output();

        public static object BIC = DB.BuiltInCategory;

        // Renumber tool option
        public class RNOpts
            : object
        {

            public RNOpts(object cat, object by_bicat = null)
            {
                this.bicat = cat;
                this._cat = revit.query.get_category(this.bicat);
                this.by_bicat = by_bicat;
                this._by_cat = revit.query.get_category(this.by_bicat);
            }

            // Renumber option name derived from option categories.
            public object name
            {
                get
                {
                    if (this.by_bicat)
                    {
                        var applocale = applocales.get_host_applocale();
                        if (applocale.lang_name.lower().Contains("english"))
                        {
                            return "{} by {}".format(this._cat.Name, this._by_cat.Name);
                        }
                        return "{} <- {}".format(this._cat.Name, this._by_cat.Name);
                    }
                    return this._cat.Name;
                }
            }
        }

        // Toggle handles for spatial elements
        public static object toggle_element_selection_handles(object target_view, object bicat, object state = true)
        {
            using (var revit.Transaction("Toggle handles")) {
                // if view has template, toggle temp VG overrides
                if (state)
                {
                    target_view.EnableTemporaryViewPropertiesMode(target_view.Id);
                }
                rr_cat = revit.query.get_subcategory(bicat, "Reference");
                try
                {
                    rr_cat.Visible[target_view] = state;
                }
                catch (Exception)
                {
                    logger.debug("Failed changing category visibility for \"%s\" to \"%s\" on view \"%s\" | %s", bicat, state, target_view.Name, vex.ToString());
                }
                rr_int = revit.query.get_subcategory(bicat, "Interior Fill");
                if (!rr_int)
                {
                    rr_int = revit.query.get_subcategory(bicat, "Interior");
                }
                try
                {
                    rr_int.Visible[target_view] = state;
                }
                catch (Exception)
                {
                    logger.debug("Failed changing interior fill visibility for \"%s\" to \"%s\" on view \"%s\" | %s", bicat, state, target_view.Name, vex.ToString());
                }
                if (!state)
                {
                    target_view.DisableTemporaryViewMode(DB.TemporaryViewMode.TemporaryViewProperties);
                }
            }
        }

        // Toggle spatial element handles for easy selection.
        public class EasilySelectableElements
            : object
        {

            public EasilySelectableElements(object target_view, object bicat)
            {
                this.supported_categories = new List<object> {
                    BIC.OST_Rooms,
                    BIC.OST_Areas,
                    BIC.OST_MEPSpaces
                };
                this.target_view = target_view;
                this.bicat = bicat;
            }

            public virtual object @__enter__()
            {
                if (this.supported_categories.Contains(this.bicat))
                {
                    toggle_element_selection_handles(this.target_view, this.bicat);
                }
                return this;
            }

            public virtual object @__exit__(object exception, object exception_value, object traceback)
            {
                if (this.supported_categories.Contains(this.bicat))
                {
                    toggle_element_selection_handles(this.target_view, this.bicat, state: false);
                }
            }
        }

        // Increment given item number by one.
        public static object increment(object number)
        {
            return coreutils.increment_str(number, expand: true);
        }

        // Get target elemnet number (might be from Number or other fields)
        public static object get_number(object target_element)
        {
            if (hasattr(target_element, "Number"))
            {
                return target_element.Number;
            }
            // determine target parameter
            var mark_param = target_element.Parameter[DB.BuiltInParameter.ALL_MODEL_MARK];
            if (target_element is DB.Level || target_element is DB.Grid)
            {
                mark_param = target_element.Parameter[DB.BuiltInParameter.DATUM_TEXT];
            }
            if (target_element is DB.Viewport)
            {
                mark_param = target_element.Parameter[DB.BuiltInParameter.VIEWPORT_DETAIL_NUMBER];
            }
            // get now
            if (mark_param)
            {
                return mark_param.AsString();
            }
        }

        // Set target elemnet number (might be at Number or other fields)
        public static object set_number(object target_element, object new_number)
        {
            if (hasattr(target_element, "Number"))
            {
                target_element.Number = new_number;
                return;
            }
            // determine target parameter
            var mark_param = target_element.Parameter[DB.BuiltInParameter.ALL_MODEL_MARK];
            if (target_element is DB.Level || target_element is DB.Grid)
            {
                mark_param = target_element.Parameter[DB.BuiltInParameter.DATUM_TEXT];
            }
            if (target_element is DB.Viewport)
            {
                mark_param = target_element.Parameter[DB.BuiltInParameter.VIEWPORT_DETAIL_NUMBER];
            }
            // set now 
            if (mark_param)
            {
                mark_param.Set(new_number);
            }
        }

        // Override element VG to transparent and halftone.
        // 
        //     Intended to mark processed renumbered elements visually.
        //     
        public static object mark_element_as_renumbered(object target_view, object room)
        {
            var ogs = DB.OverrideGraphicSettings();
            ogs.SetHalftone(true);
            ogs.SetSurfaceTransparency(100);
            target_view.SetElementOverrides(room.Id, ogs);
        }

        // Rest element VG to default.
        public static object unmark_renamed_elements(object target_view, object marked_element_ids)
        {
            foreach (var marked_element_id in marked_element_ids)
            {
                var ogs = DB.OverrideGraphicSettings();
                target_view.SetElementOverrides(marked_element_id, ogs);
            }
        }

        // Collect number:id information about target elements.
        public static object get_elements_dict(object view, object builtin_cat)
        {
            // Note: on treating viewports differently
            // tool would fail to assign a new number to viewport
            // on current sheet, if a viewport with the same
            // number exists on any other sheet
            if (BIC.OST_Viewports == builtin_cat && view is DB.ViewSheet)
            {
                return view.GetAllViewports().ToDictionary(vpid => get_number(revit.doc.GetElement(vpid)), vpid => vpid);
            }
            var all_elements = revit.query.get_elements_by_categories(new List<object> {
                builtin_cat
            });
            return all_elements.ToDictionary(x => get_number(x), x => x.Id);
        }

        // Find an appropriate replacement number for conflicting numbers.
        public static object find_replacement_number(object existing_number, object elements_dict)
        {
            var replaced_number = increment(existing_number);
            while (elements_dict.Contains(replaced_number))
            {
                replaced_number = increment(replaced_number);
            }
            return replaced_number;
        }

        // Renumber given element.
        public static object renumber_element(object target_element, object new_number, object elements_dict)
        {
            // check if elements with same number exists
            if (elements_dict.Contains(new_number))
            {
                var element_with_same_number = revit.doc.GetElement(elements_dict[new_number]);
                // make sure its not the same as target_element
                if (element_with_same_number && element_with_same_number.Id != target_element.Id)
                {
                    // replace its number with something else that is not conflicting
                    var current_number = get_number(element_with_same_number);
                    var replaced_number = find_replacement_number(current_number, elements_dict);
                    set_number(element_with_same_number, replaced_number);
                    // record the element with its new number for later renumber jobs
                    elements_dict[replaced_number] = element_with_same_number.Id;
                }
            }
            // check if target element is already listed
            // remove the existing number entry since we are renumbering
            var existing_number = get_number(target_element);
            if (elements_dict.Contains(existing_number))
            {
                elements_dict.pop(existing_number);
            }
            // renumber the given element
            logger.debug("applying %s", new_number);
            set_number(target_element, new_number);
            elements_dict[new_number] = target_element.Id;
            // mark the element visually to renumbered
            mark_element_as_renumbered(revit.active_view, target_element);
        }

        // Ask user for starting number.
        public static object ask_for_starting_number(object category_name)
        {
            return forms.ask_for_string(prompt: "Enter starting number", title: "ReNumber {}".format(category_name));
        }

        public static object _unmark_collected(object category_name, object renumbered_element_ids)
        {
            // unmark all renumbered elements
            using (var revit.Transaction("Unmark {}".format(category_name))) {
                unmark_renamed_elements(revit.active_view, renumbered_element_ids);
            }
        }

        // Main renumbering routine for elements of given category.
        public static object pick_and_renumber(object rnopts, object starting_index)
        {
            // all actions under one transaction
            var active_view = revit.active_view;
            using (var revit.TransactionGroup("Renumber {}".format(rnopts.name))) {
                // make sure target elements are easily selectable
                using (var EasilySelectableElements(active_view, rnopts.bicat))
                {
                    index = starting_index;
                    // collect existing elements number:id data
                    existing_elements_data = get_elements_dict(active_view, rnopts.bicat);
                    // list to collect renumbered elements
                    renumbered_element_ids = new List<object>();
                    // ask user to pick elements and renumber them
                    foreach (var picked_element in revit.get_picked_elements_by_category(rnopts.bicat, message: "Select {} in order".format(rnopts.name.lower())))
                    {
                        // need nested transactions to push revit to update view
                        // on each renumber task
                        using (var revit.Transaction("Renumber {}".format(rnopts.name))) {
                            // actual renumber task
                            renumber_element(picked_element, index, existing_elements_data);
                            // record the renumbered element
                            renumbered_element_ids.append(picked_element.Id);
                        }
                        index = increment(index);
                    }
                    // unmark all renumbered elements
                    _unmark_collected(rnopts.name, renumbered_element_ids);
                }
            }
        }

        // Main renumbering routine for elements of given categories.
        public static object door_by_room_renumber(object rnopts)
        {
            // all actions under one transaction
            var active_view = revit.active_view;
            using (var revit.TransactionGroup("Renumber Doors by Room")) {
                // collect existing elements number:id data
                existing_doors_data = get_elements_dict(active_view, rnopts.bicat);
                renumbered_door_ids = new List<object>();
                // make sure target elements are easily selectable
                using (var EasilySelectableElements(active_view, rnopts.bicat) && EasilySelectableElements(active_view, rnopts.by_bicat)) {
                    while (true)
                    {
                        // pick door
                        picked_door = revit.pick_element_by_category(rnopts.bicat, message: "Select a door");
                        if (!picked_door)
                        {
                            // user cancelled
                            return _unmark_collected("Doors", renumbered_door_ids);
                        }
                        // grab the associated rooms
                        _tup_1 = revit.query.get_door_rooms(picked_door);
                        from_room = _tup_1.Item1;
                        to_room = _tup_1.Item2;
                        // if more than one option for room, ask to pick
                        if (all(new List<object> {
                            from_room,
                            to_room
                        }) || !any(new List<object> {
                            from_room,
                            to_room
                        }))
                        {
                            // pick room
                            picked_room = revit.pick_element_by_category(rnopts.by_bicat, message: "Select a room");
                            if (!picked_room)
                            {
                                // user cancelled
                                return _unmark_collected("Rooms", renumbered_door_ids);
                            }
                        }
                        else
                        {
                            picked_room = from_room || to_room;
                        }
                        // get data on doors associated with picked room
                        room_doors = revit.query.get_doors(room_id: picked_room.Id);
                        room_number = get_number(picked_room);
                        using (var revit.Transaction("Renumber Door")) {
                            door_count = room_doors.Count;
                            if (door_count == 1)
                            {
                                // match door number to room number
                                renumber_element(picked_door, room_number, existing_doors_data);
                                renumbered_door_ids.append(picked_door.Id);
                            }
                            else if (door_count > 1)
                            {
                                // match door number to extended room number e.g. 100A
                                // check numbers of existing room doors and pick the next
                                room_door_numbers = (from x in room_doors
                                                     select get_number(x)).ToList();
                                new_number = coreutils.extend_counter(room_number);
                                // attempts = 1
                                // max_attempts =len([x for x in room_door_numbers if x])
                                while (room_door_numbers.Contains(new_number))
                                {
                                    new_number = increment(new_number);
                                }
                                renumber_element(picked_door, new_number, existing_doors_data);
                                renumbered_door_ids.append(picked_door.Id);
                            }
                        }
                    }
                }
            }
        }

        public static object renumber_options = new List<object> {
            RNOpts(cat: BIC.OST_Rooms),
            RNOpts(cat: BIC.OST_MEPSpaces),
            RNOpts(cat: BIC.OST_Doors),
            RNOpts(cat: BIC.OST_Doors, by_bicat: BIC.OST_Rooms),
            RNOpts(cat: BIC.OST_Walls),
            RNOpts(cat: BIC.OST_Windows),
            RNOpts(cat: BIC.OST_Parking),
            RNOpts(cat: BIC.OST_Levels),
            RNOpts(cat: BIC.OST_Grids)
        };

        static Module()
        {
            renumber_options.insert(1, RNOpts(cat: BIC.OST_Areas));
            options_dict[renumber_option.name] = renumber_option;
            door_by_room_renumber(selected_option);
            pick_and_renumber(selected_option, starting_number);
        }

        public static object renumber_options = new List<object> {
            RNOpts(cat: BIC.OST_Viewports)
        };

        public static object options_dict = OrderedDict();

        public static object selected_option_name = forms.CommandSwitchWindow.show(options_dict, message: "Pick element type to renumber:", width: 400);

        public static object selected_option = options_dict[selected_option_name];

        public static object starting_number = ask_for_starting_number(selected_option.name);
    }
}