var TableEditable2 = function () {

    return {

        //main function to initiate the module
        init: function () {
            var flag = false;
            function restoreRow(oTable, nRow) {
                var aData = oTable.fnGetData(nRow);
                var jqTds = $('>td', nRow);

                for (var i = 0, iLen = jqTds.length; i < iLen; i++) {
                    oTable.fnUpdate(aData[i], nRow, i, false);
                }

                oTable.fnDraw();
            }

            function editRow(oTable, nRow) {
                var aData = oTable.fnGetData(nRow);
                var jqTds = $('>td', nRow);
                //jqTds[0].innerHTML = '<input type="text" class="form-control required" value="' + aData[0] + '">';
                //jqTds[1].innerHTML = '<input type="text" class="form-control required" value="' + aData[1] + '">';
                jqTds[2].innerHTML = '<input type="number" class="form-control required input-small" value="' + aData[2] + '">';
                jqTds[3].innerHTML = '<input type="number" class="form-control required input-small" value="' + aData[3] + '">';
                jqTds[4].innerHTML = '<input type="number" class="form-control required input-small " value="' + aData[4] + '">';
                //jqTds[5].innerHTML = '<input type="text" class="form-control input-small " value="' + aData[5] + '" readonly>';
                //jqTds[6].innerHTML = '<input type="text" class="form-control input-small" value="' + aData[6] + '" readonly>';
                jqTds[5].innerHTML = '<a class="edit" href="">Sauvegarder</a>';
                jqTds[6].innerHTML = '<a class="cancel" href="">Annuler</a>';
            }

            function saveRow(oTable, nRow) {
                var jqInputs = $('input', nRow);
                for (var i = 0; i < 5; i++) {
                    if (jqInputs[i].value.trim().length === 0) {
                        alert("tous les champs sont obligatoires !!!");
                        flag = true;
                        return;
                    }
                }
                flag = false;
                oTable.fnUpdate(jqInputs[0].value, nRow, 0, false);
                oTable.fnUpdate(jqInputs[1].value, nRow, 1, false);
                oTable.fnUpdate(jqInputs[2].value, nRow, 2, false);
                oTable.fnUpdate(jqInputs[3].value, nRow, 3, false);
                oTable.fnUpdate(jqInputs[4].value, nRow, 4, false);
                //oTable.fnUpdate(jqInputs[5].value, nRow, 5, false);
                //oTable.fnUpdate(jqInputs[6].value, nRow, 6, false);
                oTable.fnUpdate('<a class="edit" href="">Modifier</a>', nRow, 5, false);
                oTable.fnUpdate('<a class="delete" href="">Supprimer</a>', nRow, 6, false);
                oTable.fnDraw();
            }

            function cancelEditRow(oTable, nRow) {
                var jqInputs = $('input', nRow);
                oTable.fnUpdate(jqInputs[0].value, nRow, 0, false);
                oTable.fnUpdate(jqInputs[1].value, nRow, 1, false);
                oTable.fnUpdate(jqInputs[2].value, nRow, 2, false);
                oTable.fnUpdate(jqInputs[3].value, nRow, 3, false);
                oTable.fnUpdate(jqInputs[4].value, nRow, 4, false);
                //oTable.fnUpdate(jqInputs[5].value, nRow, 5, false);
                //oTable.fnUpdate(jqInputs[6].value, nRow, 6, false);

                oTable.fnUpdate('<a class="edit" href="">Modifier</a>', nRow, 5, false);
                oTable.fnDraw();
            }

            var oTable = $('#sample_editable_1').dataTable({
                "aLengthMenu": [
                    [5, 15, 20, -1],
                    [5, 15, 20, "All"] // change per page values here
                ],
                // set the initial value
                "iDisplayLength": -1,
                
                "sPaginationType": "bootstrap",
                "oLanguage": {
                    "sLengthMenu": "_MENU_ records",
                    "oPaginate": {
                        "sPrevious": "Prev",
                        "sNext": "Next"
                    }
                },
                "aoColumnDefs": [{
                        'bSortable': false,
                        'aTargets': [0]
                    }
                ]
            });

            jQuery('#sample_editable_1_wrapper .dataTables_filter input').addClass("form-control input-medium input-inline"); // modify table search input
            jQuery('#sample_editable_1_wrapper .dataTables_length select').addClass("form-control input-small"); // modify table per page dropdown
            jQuery('#sample_editable_1_wrapper .dataTables_length select').select2({
                showSearchInput : false //hide search box with special css class
            }); // initialize select2 dropdown
            jQuery('#sample_editable_1_wrapper .dataTables_filter').addClass("hidden");
            jQuery('#sample_editable_1_wrapper .dataTables_length').addClass("hidden");
            var nEditing = null;

            $('#sample_editable_1_new').click(function (e) {
                e.preventDefault();
                var aiNew = oTable.fnAddData(['', '', '1', '', '',
                        '<a class="edit" href="">Modifier</a>', '<a class="cancel" data-mode="new" href="">Annuler</a>'
                ]);
                var nRow = oTable.fnGetNodes(aiNew[0]);
                editRow(oTable, nRow);
                nEditing = nRow;
            });

            $('#sample_editable_1 a.delete').live('click', function (e) {
                e.preventDefault();

                if (confirm("Voullez-vous vraiment supprimer cette ligne ?") == false) {
                    return;
                }

                var nRow = $(this).parents('tr')[0];
                oTable.fnDeleteRow(nRow);
                //alert("Deleted! Do not forget to do some ajax to sync with backend :)");
            });

            $('#sample_editable_1 a.cancel').live('click', function (e) {
                e.preventDefault();
                if ($(this).attr("data-mode") == "new") {
                    var nRow = $(this).parents('tr')[0];
                    oTable.fnDeleteRow(nRow);
                } else {
                    restoreRow(oTable, nEditing);
                    nEditing = null;
                }
            });

            $('#sample_editable_1 a.edit').live('click', function (e) {
                e.preventDefault();

                /* Get the row as a parent of the link that was clicked on */
                var nRow = $(this).parents('tr')[0];

                if (nEditing !== null && nEditing != nRow) {
                    /* Currently editing - but not this row - restore the old before continuing to edit mode */
                    restoreRow(oTable, nEditing);
                    editRow(oTable, nRow);
                    nEditing = nRow;
                } else if (nEditing == nRow && this.innerHTML == "Sauvegarder") {
                    /* Editing this row and want to save it */
                    saveRow(oTable, nEditing);
                    if(!flag)
                        nEditing = null;
                    //alert("Updated! Do not forget to do some ajax to sync with backend :)");
                } else {
                    /* No edit in progress - let's start one */
                    editRow(oTable, nRow);
                    nEditing = nRow;
                }
            });
        }

    };

}();