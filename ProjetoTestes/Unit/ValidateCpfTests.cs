using ProjetoTestes.Domain;

namespace ProjetoTestes.Tests;

public class ValidateCpfTests
{
    [Theory(DisplayName = "Deve Verifidar um cpf valido")]
    [InlineData("97456321558")]
    public void Deve_Verifidar_um_cpf_valido(string cpf)
    {
        
        //When
        var result = CpfValidator.ValidateCpf(cpf);

        //Then
        Assert.True(result);

    }


    [Theory(DisplayName = "Deve Verifidar um cpf com o primeiro digito diferente de zero")]
    [InlineData("71428793860")]
    public void Deve_Verifidar_um_cpf_com_o_primeiro_digito_diferente_de_zero(string cpf)
    {
        
        //When
        var result = CpfValidator.ValidateCpf(cpf);

        //Then
        Assert.True(result);

    }

    [Theory(DisplayName = "Deve Verifidar um cpf com os digitos diferentes de zero"  )]
    [InlineData("714287938600")]
    public void Deve_Verifidar_um_cpf_com_os_digitos_diferentes_de_zero(string cpf)
    {
        
        //When
        var result = CpfValidator.ValidateCpf(cpf);

        //Then
        Assert.True(result);

    }

    [Theory(DisplayName = "Deve verificar um cpf inválido")]
    [InlineData("11111111111")]
    
    public void Deve_Verifidar_um_cpf_invalido(string cpf)
    {
        
        //When
        var result = CpfValidator.ValidateCpf(cpf);

        //Then
        Assert.False(result);

    }

    [Theory(DisplayName = "Deve verificar um cpf inválido null")]
    [InlineData(null)]
    
    public void Deve_Verifidar_um_cpf_invalido_null(string? cpf)
    {
        
        //When
        var result = CpfValidator.ValidateCpf(cpf);

        //Then
        Assert.False(result);

    }

    [Theory(DisplayName = "Deve verificar um cpf inválido com mais dígitos que o necessário")]
    [InlineData("11111111111111")]
    
    public void Deve_Verifidar_um_cpf_mais_do_que_o_necessario(string? cpf)
    {
        
        //When
        var result = CpfValidator.ValidateCpf(cpf);

        //Then
        Assert.False(result);

    }

     [Theory(DisplayName = "Deve verificar um cpf inválido com mais dígitos que o 123")]
    [InlineData("123")]
    
    public void Deve_Verifidar_um_cpf_mais_do_que_o_123(string? cpf)
    {
        
        //When
        var result = CpfValidator.ValidateCpf(cpf);

        //Then
        Assert.False(result);

    }

    [Theory(DisplayName = "Deve verificar um cpf inválido com mais dígitos que o 123456789012345")]
    [InlineData("123456789012345")]
    public void Deve_Verifidar_um_cpf_mais_do_que_o_numerosmaiores(string? cpf)
    {
        
        //When
        var result = CpfValidator.ValidateCpf(cpf);

        //Then
        Assert.False(result);

    }

    [Theory(DisplayName = "Deve verificar um cpf inválido com mais dígitos que letra")]
    [InlineData("a7456321558")]
    public void Deve_Verifidar_um_cpf_mais_do_que_letra(string? cpf)
    {
        
        //When
        var result = CpfValidator.ValidateCpf(cpf);

        //Then
        Assert.False(result);

    }

}
