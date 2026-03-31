import { Component, OnInit, ChangeDetectorRef } from '@angular/core';
import { HttpClient, HttpHeaders } from '@angular/common/http';
import { CommonModule } from '@angular/common';
import { FormsModule } from '@angular/forms';
import { firstValueFrom } from 'rxjs';

@Component({
  selector: 'app-root',
  standalone: true,
  imports: [CommonModule, FormsModule],
  templateUrl: './app.html',
  styleUrls: ['./app.css']
})
export class App implements OnInit {
  // URLs das nossas APIs
  private readonly lancamentosApiUrl = 'http://localhost:5001/api/v1'; // Ajuste a porta se necessário
  private readonly consolidadoApiUrl = 'http://localhost:5002/api/v1'; // Ajuste a porta se necessário

  tokenJwt: string = '';
  saldoAtual: number | null = null;

  // Modelo do formulário
  novoLancamento = {
    tipo: 'Credito',
    valor: 0,
    descricao: ''
  };

  constructor(private http: HttpClient, private cdr: ChangeDetectorRef) { }

  async ngOnInit() {
    // Ao abrir a tela, fazemos o "Login" falso para pegar o Token de autorização
    await this.autenticar();
    await this.buscarSaldo();
  }

  // 1. Pega o Token
  async autenticar() {
    try {
      const response: any = await firstValueFrom(
        this.http.post(`${this.lancamentosApiUrl}/auth/login`, {})
      );
      this.tokenJwt = response.token;
    } catch (error) {
      console.error('Erro ao autenticar', error);
    }
  }

  // 2. Busca o Saldo no Consolidado.API (A Query super rápida)
  async buscarSaldo() {
    try {
      const headers = new HttpHeaders({
        'Authorization': `Bearer ${this.tokenJwt}`
      });

      const hoje = new Date().toISOString().split('T')[0]; // Pega a data YYYY-MM-DD
      const response: any = await firstValueFrom(
        this.http.get(`${this.consolidadoApiUrl}/Consolidado/${hoje}`, { headers })
      );

      console.log(response)

      this.saldoAtual = response.saldo;

      this.cdr.detectChanges();
    } catch (error) {
      console.error('Erro ao buscar saldo', error);
    }
  }

  // 3. Envia o Lançamento para a Lancamentos.API (O Command)
  async registrarLancamento() {
    if (this.novoLancamento.valor <= 0) {
      alert('O valor deve ser maior que zero!');
      return;
    }

    const headers = new HttpHeaders({
      'Authorization': `Bearer ${this.tokenJwt}`
    });

    try {
      await firstValueFrom(
        this.http.post(`${this.lancamentosApiUrl}/Lancamentos`, this.novoLancamento, { headers })
      );

      alert('Lançamento registrado com sucesso!');

      // Limpa o formulário
      this.novoLancamento.valor = 0;
      this.novoLancamento.descricao = '';

      // Aguarda um pequeno instante para dar tempo do Worker processar, e busca o saldo novamente
      setTimeout(() => this.buscarSaldo(), 500);

    } catch (error) {
      console.error('Erro ao registrar', error);
      alert('Erro ao registrar lançamento.');
    }
  }
}