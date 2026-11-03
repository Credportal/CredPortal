// Please see documentation at https://learn.microsoft.com/aspnet/core/client-side/bundling-and-minification
// for details on configuring this project to bundle and minify static web assets.

// Write your JavaScript code.

$(document).ready(function () {
    getDataTable('#table-registos');
    getDataTable('#table-usuarios');
    getDataTable('#table-agencias');
    getDataTable('#table-pendentes');

    $('.btn-total-registos').click(function () {
        var usuarioId = $(this).attr('usuario-id');

        $.ajax({
            type: 'GET',
            url: '/Usuario/listarContactosPorUsuarioId/' + usuarioId,
            success: function (result) {
                $("#listaContactoUsuario").html(result);                
                $('#modalContactosUsuario').modal();
                getDataTable('#table-registos-usuario');
            }
        });        
    });
})

function getDataTable(id)
{
    $(id).DataTable({
        "ordering": true,
        "paging": true,
        "searching": true,
        "oLanguage": {
            "sEmptyTable": "Nenhum registo encontrado na tabela",
            "sInfo": "_START_ até _END_ de _TOTAL_",
            "sInfoEmpty": "0 até 0 de 0",
            "sInfoFiltered": "(_MAX_)",
            "sInfoPostFix": "",
            "sInfoThousands": ".",
            "sLengthMenu": "_MENU_",
            "sLoadingRecords": "Carregando...",
            "sProcessing": "Processando...",
            "sZeroRecords": "Nenhum registo encontrado",
            "sSearch": "Pesquisar",
            "oAria": {
                "sSortAscending": ": Ordenar colunas de forma ascendente",
                "sSortDescending": ": Ordenar colunas de forma descendente"
            }
        }
    });
}
$('.close-alert').click(function () {
    $('.alert').hide('hide');
})