﻿using System.Collections.Generic;

namespace CinemaManagement.DTOs
{
    public class TicketDTO
    {
        public TicketDTO()
        {
            this.TicketBillInfoes = new List<TicketBillInfoDTO>();
        }
        public int Id { get; set; }
        public int ShowtimeId { get; set; }
        public int SeatId { get; set; }
        public decimal Price { get; set; }

        public SeatDTO Seat { get; set; }
        public ShowtimeDTO Showtime { get; set; }
        public IList<TicketBillInfoDTO> TicketBillInfoes { get; set; }
    }
}
